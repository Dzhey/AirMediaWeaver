using System.Net;

namespace AirMedia.Core.Controller.WebService.Http
{
    public interface IHttpRequestHandler
    {
        void HandleHttpRequest(HttpServer server, HttpListenerContext context);
    }
}