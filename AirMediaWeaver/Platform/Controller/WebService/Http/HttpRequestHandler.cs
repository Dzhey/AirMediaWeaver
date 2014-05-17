using System;
using System.Net;
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

        private const int UriCodePublishedTracks = 1;

        private static readonly UriMatcher UriMatcher;

        private readonly IHttpContentProvider _httpContentProvider;

        static HttpRequestHandler()
        {
            UriMatcher = new UriMatcher(UriMatcher.NoMatch);
            UriMatcher.AddURI(Consts.UriPublicationsFragment, 
                Consts.UriTracksFragment, UriCodePublishedTracks);
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

        public void HandleHttpRequest(HttpServer server, HttpListenerContext context)
        {
            var uri = ToContentUri(context.Request.Url);

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
                AmwLog.Error(LogTag, string.Format(
                    "Error trying to retrieve http track info; Message: \"{0}\"", e.Message), e);

                errorCode = ErrorInternal;
            }

            AmwLog.Verbose(LogTag, string.Format("published tracks response containing " +
                                                 "{0} tracks", info.Length));

            return new HttpResponsePublishedTracks
                {
                    TrackInfo = info
                };
        }

        protected void PerformResponse(HttpListenerResponse contextResponse, HttpResponse responseData, 
            int errorCode = 0, string reasonPhrase = null)
        {
            AmwLog.Debug(LogTag, "perofrming http response..");

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
    }
}