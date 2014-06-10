using System;
using System.Collections.Generic;
using System.Net;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Logger;

namespace AirMedia.Core.Controller.WebService.Http
{
    public class HttpServer : IDisposable, ObtainHttpListenerContextRequest.IHttpServeCallbacks
    {
        public static readonly string LogTag = typeof (HttpServer).Name;

        private static readonly string[] ContentUris = new[]
            {
                // Publications
                Consts.UriPublicationsFragment,
                Consts.UriPeersFragment
            };

        public bool IsListening
        {
            get { return _httpListener != null && _httpListener.IsListening; }
        }

        private HttpListener _httpListener;
        private bool _isDisposed;
        private readonly RequestResultListener _requestResultListener;
        private readonly IHttpRequestHandler _httpRequestHandler;

        public HttpServer(IHttpRequestHandler httpRequestHandler)
        {
            _httpRequestHandler = httpRequestHandler;
            _httpListener = new HttpListener();
            int random = new Random().Next(int.MaxValue);
            string listenerTag = string.Format("{0}_{1}", typeof(HttpServer).Name, random);
            _requestResultListener = new RequestResultListener(listenerTag);

            _requestResultListener.RegisterResultHandler(
                typeof(ObtainHttpListenerContextRequest), OnHttpListenerContextRequestFinished);
        }

        public bool TryStart(int ipAddress)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("http server already disposed");
            }

            if (_httpListener.IsListening)
            {
                AmwLog.Warn(LogTag, "http listener is already listening for connections");
                return true;
            }

            string ipAddressString = new IPAddress(BitConverter.GetBytes(ipAddress)).ToString();
            var prefixes = RetrieveHttpPrefixes(ipAddressString);
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
                AmwLog.Error(LogTag, e, string.Format("can't setup http listener: {0}; error code: " +
                                                      "{1}", e.Message, e.ErrorCode));

                return false;
            }
            catch (ObjectDisposedException e)
            {
                AmwLog.Error(LogTag, e, "can't setup http listener: object disposed");

                return false;
            }

            _requestResultListener.SubmitDedicatedRequest(new ObtainHttpListenerContextRequest(this));

            return true;
        }

        public void Stop()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("http server already disposed");
            }

            if (IsListening == false)
            {
                AmwLog.Warn(LogTag, "http listener already stopped");
                return;
            }

            AmwLog.Debug(LogTag, "stopping http listener..");
            _httpListener.Stop();
            AmwLog.Info(LogTag, "http listener stopped");
        }

        protected virtual string[] RetrieveHttpPrefixes(string ipAddress)
        {
            var result = new List<string>();

            AmwLog.Debug(LogTag, string.Format("building http prefixes for \"{0}\"", ipAddress));

            foreach (var contentUri in ContentUris)
            {
                result.Add(string.Format("http://{0}:{1}/{2}/", 
                    ipAddress, Consts.DefaultHttpPort, contentUri));
            }

            return result.ToArray();
        }

        public void OnRequestContextObtained(HttpListenerContext context)
        {
            AmwLog.Info(LogTag, "http listener context obtained, handling request..");

            if (_httpListener.IsListening)
            {
                AmwLog.Debug(LogTag, "http listener proceed listening");
                _requestResultListener.SubmitDedicatedRequest(new ObtainHttpListenerContextRequest(this));
            }
            else
            {
                AmwLog.Debug(LogTag, "http listener stopped");
            }

            _httpRequestHandler.HandleHttpRequest(this, context);

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
                if (IsListening)
                {
                    Stop();
                }

                _requestResultListener.RemoveResultHandler(typeof(ObtainHttpListenerContextRequest));
                _requestResultListener.Dispose();
                _httpListener.Close();
                _httpListener = null;
            }

            _isDisposed = true;
        }
    }
}