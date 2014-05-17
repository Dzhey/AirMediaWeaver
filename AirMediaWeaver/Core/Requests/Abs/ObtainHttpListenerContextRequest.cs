using System;
using System.Net;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Requests.Abs
{
    public class ObtainHttpListenerContextRequest : AbsRequest
    {
        public interface IHttpServeCallbacks
        {
            void OnRequestContextObtained(HttpListenerContext context);
            HttpListenerContext GetContext();
        }

        public class RequestResult : LoadRequestResult<HttpListenerContext>
        {
            public RequestResult(int resultCode, HttpListenerContext resultData) 
                : base(resultCode, resultData)
            {
            }

            internal RequestResult(int resultCode, HttpListenerContext resultData, Exception risenException) 
                : base(resultCode, resultData, risenException)
            {
            }
        }

        private readonly IHttpServeCallbacks _callbacks;

        public ObtainHttpListenerContextRequest(IHttpServeCallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        protected override BaseRequestResult ExecuteImpl(out RequestStatus status)
        {
            Exception risenException = null;
            status = RequestStatus.Ok;
            try
            {
                var context = _callbacks.GetContext();

                AmwLog.Debug(LogTag, "incoming http request obtained");

                _callbacks.OnRequestContextObtained(context);

                return new RequestResult(BaseRequestResult.ResultCodeOk, context);
            }
            catch (HttpListenerException e)
            {
                AmwLog.Error(LogTag, string.Format("Cant obtain HttpListenerContext: " +
                                                   "{0}; Error code: {1}", e.Message, e.ErrorCode), e);
                risenException = e;
            }
            catch (ObjectDisposedException e)
            {
                AmwLog.Error(LogTag, string.Format("Cant obtain HttpListenerContext; " +
                                                   "HttpListener is disposed."), e);
                risenException = e;
            }
            catch (InvalidOperationException e)
            {
                AmwLog.Error(LogTag, string.Format("Cant obtain HttpListenerContext; Usually indicates " +
                                                   "that HttpListener is stopped."), e);
                risenException = e;
            }

            status = RequestStatus.Failed;

            return new RequestResult(BaseRequestResult.ResultCodeFailed, null, risenException);
        }
    }
}