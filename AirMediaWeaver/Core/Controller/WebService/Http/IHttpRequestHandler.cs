using System;
using System.Net;
using AirMedia.Core.Controller.WebService.Model;

namespace AirMedia.Core.Controller.WebService.Http
{
    public interface IHttpRequestHandler
    {
        event EventHandler<AuthPacketReceivedEventArgs> AuthPacketReceived;
        void HandleHttpRequest(HttpServer server, HttpListenerContext context);
    }
}