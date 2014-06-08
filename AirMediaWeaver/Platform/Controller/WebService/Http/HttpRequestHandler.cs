using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AirMedia.Core;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using Android.Content;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Controller.WebService.Http
{
    public class HttpRequestHandler : IHttpRequestHandler
    {
        public static readonly string LogTag = typeof (HttpRequestHandler).Name;

        public const int ErrorUndefinedRequest = -1;
        public const int ErrorInvalidRequest = -2;
        public const int ErrorInternal = -3;
        public const int ErrorInvalidTrackGuid = 1;

        private const int UriCodePublishedTracks = 1;
        private const int UriCodeTrackStream = 2;

        private static readonly UriMatcher UriMatcher;

        private readonly IHttpContentProvider _httpContentProvider;

        static HttpRequestHandler()
        {
            UriMatcher = new UriMatcher(UriMatcher.NoMatch);

            // Request track publications info
            UriMatcher.AddURI(Consts.UriPublicationsFragment,
                Consts.UriTracksFragment, UriCodePublishedTracks);

            // Request track data stream
            UriMatcher.AddURI(Consts.UriPublicationsFragment,
                Consts.UriTracksFragment + "/*", UriCodeTrackStream);
        }

        public HttpRequestHandler(IHttpContentProvider contentProvider)
        {
            _httpContentProvider = contentProvider;
        }

        private Uri ToContentUri(System.Uri httpUri)
        {
            var builder = new StringBuilder();
            foreach (var segment in httpUri.Segments)
            {
                builder.Append(segment);
            }

            return Uri.Parse(string.Format("{0}:/{1}", ContentResolver.SchemeContent, builder));
        }

        private void PrintResponse(HttpListenerResponse response)
        {
            AmwLog.Debug(LogTag, string.Format("output response prints; " + 
                                               "protocol version: {0}" +
                                               "status code: \"{1}\"; " +
                                               "content-type: \"{2}\"; " +
                                               "content-length: \"{3}\"",
                                               response.ProtocolVersion,
                                               response.StatusCode,
                                               response.ContentType,
                                               response.ContentLength64));

            for (int i = 0; i < response.Headers.Count; i++)
            {
                AmwLog.Debug(LogTag, string.Format("response header: \"{0}: {1}\"",
                    response.Headers.GetKey(i), response.Headers.Get(i)));
            }
        }

        private void PrintRequest(HttpListenerRequest request)
        {
            AmwLog.Debug(LogTag, string.Format("processing incoming http request; method: \"{0}\"; " +
                                               "user-agent: \"{1}\"; url: \"{2}\"",
                                               request.HttpMethod,
                                               request.UserAgent,
                                               request.Url));

            for (int i = 0; i < request.Headers.Count; i++)
            {
                AmwLog.Debug(LogTag, string.Format("request header: \"{0}: {1}\"",
                    request.Headers.GetKey(i), request.Headers.Get(i)));
            }
        }

        public void HandleHttpRequest(HttpServer server, HttpListenerContext context)
        {
            var uri = ToContentUri(context.Request.Url);

            PrintRequest(context.Request);

            int errorCode = 0;
            var contextResponse = context.Response;

            switch (UriMatcher.Match(uri))
            {
                case UriMatcher.NoMatch:
                    PerformResponse(contextResponse, null, ErrorInvalidRequest, "no match");
                    break;

                case UriCodePublishedTracks:
                    var response = CreatePublshedTracksResponse(out errorCode);
                    PerformResponse(contextResponse, response, errorCode);
                    break;

                case UriCodeTrackStream:
                    Guid trackGuid;
                    if (Guid.TryParse(uri.LastPathSegment, out trackGuid) == false)
                    {
                        PerformResponse(contextResponse, null, ErrorInvalidTrackGuid, "invalid track id");
                    }
                    else
                    {
                        PerformTrackStreamResponse(context, trackGuid.ToString());
                    }
                    break;

                default:
                    PerformResponse(contextResponse, null, ErrorUndefinedRequest, 
                        "can't determine request type");
                    break;
            }
        }

        protected HttpResponsePublishedTracks CreatePublshedTracksResponse(out int errorCode)
        {
            AmwLog.Verbose(LogTag, "creating published tracks response..");
            errorCode = 0;
            var info = new HttpBaseTrackInfo[0];
            try
            {
                info = _httpContentProvider.GetBaseTrackPublicationsInfo();
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, e, string.Format(
                    "Error trying to retrieve http track info; Message: \"{0}\"", e.Message));

                errorCode = ErrorInternal;
            }

            AmwLog.Verbose(LogTag, string.Format("published tracks response containing " +
                                                 "{0} tracks", info.Length));

            return new HttpResponsePublishedTracks
                {
                    TrackInfo = info
                };
        }

        protected void PerformTrackStreamResponse(HttpListenerContext context, string trackGuid)
        {
            AmwLog.Debug(LogTag, "perofrming http track stream response..");

            var request = context.Request;
            var response = context.Response;
            var trackInfo = _httpContentProvider.GetHttpTrackInfo(trackGuid);

            if (trackInfo == null || trackInfo.FilePath == null)
            {
                PerformInternalFailureRequest(response, "can't retrieve track info");
                return;
            }

            AmwLog.Verbose(LogTag, string.Format("http track stream response " +
                                                 "model built: \"{0}\"", trackInfo));

            response.ContentEncoding = Encoding.UTF8;
            response.AddHeader("Accept-Ranges", "bytes");

            ContentRangeHeaderValue requestedRange = null;
            string headerRange = request.Headers.Get(HttpRequestHeader.Range.ToString());
            if (headerRange != null)
            {

                if (HttpContentRangeParser.TryParseRange(headerRange, 
                    trackInfo.ContentLength, out requestedRange) == false)
                {
                    AmwLog.Warn(LogTag, string.Format("can't parse requested content " +
                                                      "range: \"{0}\"", headerRange));

                    response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                    response.StatusDescription = "Requested Range not Satisfiable";
                    return;
                }
                else
                {
                    AmwLog.Debug(LogTag, string.Format("accepted range: {0}", headerRange));
                }
            }

            response.Headers.Add("Content-Disposition", string.Format(
                "attachment; filename=\"{0}\"", trackInfo.FileName));
            response.ContentType = trackInfo.ContentType ?? "application/octet-stream";
            response.StatusCode = (int) (requestedRange == null
                                             ? HttpStatusCode.OK
                                             : HttpStatusCode.PartialContent);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                using (var stream = new FileStream(trackInfo.FilePath,
                                                   FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var writer = HttpResponseStreamWriter.NewInstance(trackInfo.ContentLength,
                        response.OutputStream, stream, requestedRange);

                    var responseRangeHeader = writer.ComputeContentLength();
                    response.ContentLength64 = responseRangeHeader.Length ?? 0;
                    if (requestedRange != null)
                    {
                        response.AddHeader("Content-Range", responseRangeHeader.ToString());
                    }

                    PrintResponse(response);
                    AmwLog.Verbose(LogTag, "writing track data to output http stream..");
                    writer.Write();
                }

                AmwLog.Verbose(LogTag, "track data written, closing stream..");
                response.Close();
            }
            catch (IOException e)
            {
                AmwLog.Warn(LogTag, string.Format(
                    "IOException caught during http playback streaming: {0}", e.Message), e.ToString());
            }
            sw.Stop();

            AmwLog.Verbose(LogTag, string.Format("track data written; {0} millis elapsed", sw.ElapsedMilliseconds));
            AmwLog.Debug(LogTag, "http track stream response finished");
        }

        protected void PerformResponse(HttpListenerResponse contextResponse, HttpResponse responseData, 
            int errorCode = 0, string reasonPhrase = null)
        {
            AmwLog.Debug(LogTag, "performing http response..");

            var model = HttpResponseFactory.CreateResponse(errorCode, reasonPhrase, responseData);
            var response = HttpResponseFactory.PackResponse(model);

            AmwLog.Debug(LogTag, string.Format("http response created; type: {0}; error: {1}; " +
                                               "reason: {2}", model.ResponseType, errorCode, reasonPhrase));

            contextResponse.ContentType = "text/json";
            contextResponse.StatusCode = (int) (errorCode == 0 ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            contextResponse.ContentEncoding = Encoding.UTF8;

            var data = Encoding.UTF8.GetBytes(response);
            contextResponse.ContentLength64 = data.Length;
            contextResponse.OutputStream.Write(data, 0, data.Length);

            AmwLog.Debug(LogTag, "http response finished");
        }

        protected void PerformInternalFailureRequest(HttpListenerResponse contextResponse, 
            string reasonPhrase = null)
        {
            AmwLog.Debug(LogTag, "performing http internal failure response..");

            contextResponse.ContentType = "text/plain";
            contextResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
            contextResponse.ContentEncoding = Encoding.UTF8;

            var data = Encoding.UTF8.GetBytes(string.Format("Internal error\n{0}", reasonPhrase));
            contextResponse.ContentLength64 = data.Length;
            contextResponse.OutputStream.Write(data, 0, data.Length);

            AmwLog.Debug(LogTag, "http internal failure response finished");
        }
    }
}