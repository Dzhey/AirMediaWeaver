using System;
using System.Linq;
using System.Threading;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Core.Utils;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class AbsRequest : IDisposable
    {
        private string _logTag;
        private int? _requestId;
        private RequestResult _requestResult;
        private readonly RequestStatus[] _statusFinished = new[]
            {
                RequestStatus.Ok, 
                RequestStatus.Cancelled, 
                RequestStatus.Failed
            };

        private Future<RequestResult> _futureResult;
        private bool _isDisposed;

        public string LogTag
        {
            get
            {
                if (_logTag == null)
                {
                    _logTag = GetType().Name;
                }

                return _logTag;
            }
        }

        public bool HasRequestId { get { return _requestId.HasValue; } }

        public int RequestId
        {
            get
            {
                if (_requestId.HasValue == false)
                {
                    throw new InvalidOperationException("request id is not initialized yet");
                }
                return _requestId.Value;
            }

            set
            {
                if (_requestId.HasValue)
                {
                    throw new InvalidOperationException("Request id is already specified");
                }

                _requestId = value;
            }
        }

        public CancellationToken CancellationToken { get; set; }
        public RequestStatus Status { get; protected set; }
        public bool IsCancelled { get; private set; }
        public string ActionTag { get; set; }
        public object Payload { get; set; }

        public Future<RequestResult> FutureResult
        {
            get
            {
                lock (this)
                {
                    if (_futureResult == null)
                    {
                        _futureResult = new Future<RequestResult>();
                        if (IsFinished)
                        {
                            _futureResult.SetValue(_requestResult);
                        }
                    }
                }

                return _futureResult;
            }
        } 

        public RequestResult Result
        {
            get
            {
                if (IsFinished == false)
                {
                    throw new InvalidOperationException("request is not finished yet");
                }

                return _requestResult;
            }

            private set { _requestResult = value; }
        }

        public bool IsFinished
        {
            get { return _statusFinished.Contains(Status); }
        }

        public event EventHandler<ResultEventArgs> ResultEvent;
        public event EventHandler<UpdateEventArgs> UpdateEvent; 

        public void Cancel()
        {
            if (IsCancelled) return;

            AmwLog.Debug(LogTag, string.Format("request \"{0}\" cancel requested", GetType().Name));
            IsCancelled = true;
            CancelRequestImpl();
        }

        public virtual ResultEventArgs GetRequestResultEventArgs()
        {
            return CreateResultEventArgs();
        }

        public RequestResult Execute()
        {
            if (Status != RequestStatus.Pending)
            {
                throw new InvalidOperationException("request is already executed");
            }

            Status = RequestStatus.InProgress;

            RequestResult result;

            RequestStatus resultStatus;

            try
            {
                OnPreExecute();

                result = ExecuteImpl(out resultStatus);

                OnPostExecute(result, resultStatus);
            } 
            catch (Exception e)
            {
                result = new RequestResult(RequestResult.ResultCodeFailed, e);
                resultStatus = RequestStatus.Failed;
                AmwLog.Error(LogTag, "Exception caught while executing request: {0}; {1}", e, GetType().Name);
            }

            if (result.HasResultCode == false)
            {
                AmwLog.Error(LogTag, result, "request result has no result code specified");
                result = new RequestResult(RequestResult.ResultCodeFailed) {ErrorMessage = "no result code specified"};
                resultStatus = RequestStatus.Failed;
            }

            switch (resultStatus)
            {
                case RequestStatus.Ok:
                    if (result.ResultCode == RequestResult.ResultCodeFailed
                        || result.ResultCode == RequestResult.ResultCodeCancelled)
                    {
                        throw new ApplicationException("inconsistent request status and result code detected");
                    }
                    break;

                case RequestStatus.Failed:
                    if (result.ResultCode == RequestResult.ResultCodeOk
                        || result.ResultCode == RequestResult.ResultCodeOkUpdate)
                    {
                        throw new ApplicationException("inconsistent request status and result code detected");
                    }
                    break;

                case RequestStatus.Cancelled:
                    break;

                default:
                    throw new ApplicationException(string.Format(
                        "ExecuteImpl has returned invalid status: {0}", Status));
            }

            Status = resultStatus;
            Result = result;

            NotifyRequestResult(CreateResultEventArgs());

            return result;
        }

        /// <summary>
        /// Do an actual request job.
        /// </summary>
        /// <param name="status">One of request finish statuses</param>
        /// <returns>Result to assing with this request</returns>
        protected abstract RequestResult ExecuteImpl(out RequestStatus status);

        protected virtual void OnPreExecute()
        {
        }

        protected virtual void OnPostExecute(RequestResult result, RequestStatus status)
        {
        }

        protected virtual void CancelRequestImpl()
        {
        }

        protected virtual ResultEventArgs CreateResultEventArgs()
        {
            return new ResultEventArgs(this, Result);
        }

        protected virtual UpdateEventArgs CreateUpdateEventArgs(UpdateData updateData)
        {
            return new UpdateEventArgs(this, updateData);
        }

        /// <summary>
        /// Should be invoked manually whenever update callbacks needed.
        /// </summary>
        protected virtual void NotifyRequestUpdate(UpdateEventArgs args)
        {
            if (UpdateEvent != null)
            {
                UpdateEvent(this, args);
            }
        }

        /// <summary>
        /// Automatically invoked on request execution finish.
        /// </summary>
        protected virtual void NotifyRequestResult(ResultEventArgs args)
        {
            if (ResultEvent != null)
            {
                ResultEvent(this, args);
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
                if (_futureResult != null)
                {
                    _futureResult.Dispose();
                }
                _requestResult = null;
                _futureResult = null;
                ResultEvent = null;
                UpdateEvent = null;
            }

            _isDisposed = true;
        }
    }
}
