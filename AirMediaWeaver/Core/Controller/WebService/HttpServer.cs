using System;
using System.Collections.Generic;
using System.Net;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Logger;

namespace AirMedia.Core.Controller.WebService
{
    public class HttpServer : IDisposable, ObtainHttpListenerContextRequest.IHttpServeCallbacks
    {
        public static readonly string LogTag = typeof (HttpServer).Name;

        private static readonly string[] ContentUris = new[]
            {
                "/content/publications/"
            };

        public bool IsListening
        {
            get { return _httpListener.IsListening; }
        }

        private HttpListener _httpListener;
        private bool _isDisposed;
        private readonly RequestResultListener _requestResultListener;

        public HttpServer()
        {
            _httpListener = new HttpListener();
            int random = new Random().Next(int.MaxValue);
            string listenerTag = string.Format("{0}_{1}", typeof(HttpServer).Name, random);
            _requestResultListener = new RequestResultListener(listenerTag);

            _requestResultListener.RegisterResultHandler(
                typeof(ObtainHttpListenerContextRequest), OnHttpListenerContextRequestFinished);
        }

        public bool TryStart()
        {
            if (_httpListener.IsListening)
            {
                AmwLog.Warn(LogTag, "http listener is already listening for connections");
                return true;
            }

            var prefixes = RetrieveHttpPrefixes();
            _httpListener.Prefixes.Clear();
            foreach (var prefix in prefixes)
            {
                AmwLog.Debug(LogTag, string.Format("using http prefix: \"{0}\"", prefix));
                _httpListener.Prefixes.Add(prefix);
            }

            if (prefixes.Length == 0)
            {
                AmwLog.Error(LogTag, "unable to start network listener: no any http prefix provided");
                throw new WebServiceException("unable to start network listener: no any http prefix provided");
            }

            AmwLog.Debug(LogTag, "starting http listener..");
            try
            {
                _httpListener.Start();
                AmwLog.Info(LogTag, "http listener sucessfully started");
            }
            catch (HttpListenerException e)
            {
                AmwLog.Error(LogTag, string.Format("can't setup http listener: {0}; error code: " +
                                                   "{1}", e.Message, e.ErrorCode), e);

                return false;
            }
            catch (ObjectDisposedException e)
            {
                AmwLog.Error(LogTag, "can't setup http listener: object disposed", e);

                return false;
            }

            _requestResultListener.SubmitRequest(new ObtainHttpListenerContextRequest(this), true);

            return true;
        }

        public void Stop()
        {
            AmwLog.Debug(LogTag, "stopping http listener..");
            _httpListener.Stop();
            AmwLog.Info(LogTag, "http listener stopped");
        }

        protected virtual string[] RetrieveHttpPrefixes()
        {
            var result = new List<string>();

            // TODO: determine appropriate ip address
            string address = "192.168.1.21:6113";

            AmwLog.Debug(LogTag, string.Format("building http prefixes for \"{0}\"", address));


            foreach (var contentUri in ContentUris)
            {
                result.Add(string.Format("http://{0}{1}", address, contentUri));
            }

            return result.ToArray();
        }

        public void OnRequestContextObtained(HttpListenerContext context)
        {
            AmwLog.Info(LogTag, "http listener context obtained, handling request..");

            if (_httpListener.IsListening)
            {
                AmwLog.Debug(LogTag, "http listener proceed listening");
                _requestResultListener.SubmitRequest(
                    new ObtainHttpListenerContextRequest(this), true);
            }
            else
            {
                AmwLog.Debug(LogTag, "http listener stopped");
            }

            context.Response.StatusCode = 404;
            context.Response.Close();
            AmwLog.Info(LogTag, "http response finished");
        }

        public HttpListenerContext GetContext()
        {
            return _httpListener.GetContext();
        }

        protected virtual void OnHttpListenerContextRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, string.Format("obtain http listener context " +
                                                  "request failed with code: {0}", args.Result.ResultCode));

            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _requestResultListener.RemoveResultHandler(typeof(ObtainHttpListenerContextRequest));
                _httpListener = null;
            }

            _isDisposed = true;
        }
    }
}