using System.Net;
using System.Text;
using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Controller.WebService.Http;
using Android.Content;
using Consts = AirMedia.Core.Consts;
using AndroidUri = Android.Net.Uri;

namespace AirMedia.Platform.Controller.WebService
{
    public class HttpRequestHandler : IHttpRequestHandler
    {
        public const int ErrorUndefinedRequest = -1;
        public const int ErrorInvalidRequest = -2;

        private const int UriCodePublishedTracks = 1;

        private static readonly UriMatcher UriMatcher;

        static HttpRequestHandler()
        {
            UriMatcher = new UriMatcher(UriMatcher.NoMatch);
            UriMatcher.AddURI(Consts.UriPublicationsFragment, 
                Consts.UriTracksFragment, UriCodePublishedTracks);
        }

        private AndroidUri ToContentUri(System.Uri httpUri)
        {
            var builder = new StringBuilder();
            foreach (var segment in httpUri.Segments)
            {
                builder.Append(segment);
            }

            return AndroidUri.Parse(string.Format("{0}:/{1}", ContentResolver.SchemeContent, builder));
        }

        public void HandleHttpRequest(HttpServer server, HttpListenerContext context)
        {
            var uri = ToContentUri(context.Request.Url);

            var contextResponse = context.Response;

            switch (UriMatcher.Match(uri))
            {
                case UriMatcher.NoMatch:
                    PerformResponse(contextResponse, null,
                        HttpResponseFactory.HttpResponseNone, ErrorInvalidRequest);
                    break;

                case UriCodePublishedTracks:
                    var response = new HttpResponsePublishedTracks
                        {
                            FakeData = "hello!"
                        };
                    PerformResponse(contextResponse, response, 
                        HttpResponseFactory.HttpResponsePublishedTracks);
                    break;

                default:
                    PerformResponse(contextResponse, null,
                        HttpResponseFactory.HttpResponseNone, ErrorUndefinedRequest);
                    break;
            }
        }

        public void PerformResponse(HttpListenerResponse contextResponse, HttpResponse responseData, 
            int responseCode, int errorCode = 0, string reasonPhrase = null)
        {
            var model = HttpResponseFactory.CreateResponse(errorCode, reasonPhrase, responseData);
            var response = HttpResponseFactory.PackResponse(model);

            contextResponse.ContentType = "text/json";
            contextResponse.StatusCode = (int) (errorCode == 0 ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            contextResponse.ContentEncoding = System.Text.Encoding.UTF8;

            var data = System.Text.Encoding.UTF8.GetBytes(response);
            contextResponse.ContentLength64 = data.Length;
            contextResponse.OutputStream.Write(data, 0, data.Length);
        }
    }
}