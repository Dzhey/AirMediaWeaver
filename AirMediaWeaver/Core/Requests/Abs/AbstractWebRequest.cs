using System.Net;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class AbsWebRequest : AbsRequest
    {
        public class WebRequestResult : RequestResult
        {
            public const int ResultCodeNoConnection = 91000;
            public int ErrorType { get; set; }

            public WebRequestResult(int resultCode)
                : base(resultCode)
            {
            }
        }

        public const HttpStatusCode StatusCodeDefault = HttpStatusCode.NoContent;

        public bool ProtocolError { get; private set; }
        public HttpStatusCode StatusCode { get; protected set; }
        public WebExceptionStatus? WebExStatus { get; private set; }
        public HttpWebResponse Response { get; private set; }
        public string Url { get; protected set; }
        public bool HasConnected
        {
            get
            {
                if (WebExStatus == null) return false;

                switch (WebExStatus.Value)
                {
                    case WebExceptionStatus.ConnectFailure:
                        return false;

                    case WebExceptionStatus.NameResolutionFailure:
                        return false;

                    case WebExceptionStatus.Timeout:
                        return false;
                }

                return true;
            }
        }

        protected AbsWebRequest(string url = null)
        {
            Url = url;
            StatusCode = StatusCodeDefault;
        }

        protected sealed override RequestResult ExecuteImpl(out RequestStatus status)
        {
            try
            {
                return ExecuteWebRequest(out status, Url);
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;
                if (response != null)
                {
                    Response = response;
                }

                WebExStatus = e.Status;

                if (WebExStatus == WebExceptionStatus.ProtocolError)
                {
                    ProtocolError = true;
                }

                AmwLog.Error(LogTag, e, "Web exception caught. Status: \"{0}\". Response: \"{1}\". " +
                    "Message: \"{2}\"", e.Status, e.Response, e.Message);

                status = RequestStatus.Failed;

                return new WebRequestResult(HasConnected ? 
                    RequestResult.ResultCodeFailed : WebRequestResult.ResultCodeNoConnection);
            }
        }

        protected abstract WebRequestResult ExecuteWebRequest(out RequestStatus status, string url);
    }
}
