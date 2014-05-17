using System.Net;

namespace AirMedia.Core.Controller.WebService
{
    public interface IHttpRequestHandler
    {
        void HandleHttpRequest(HttpServer server, HttpListenerContext context);
    }
}