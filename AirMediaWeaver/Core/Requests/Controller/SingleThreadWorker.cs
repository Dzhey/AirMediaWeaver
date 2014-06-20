using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Controller
{
    public class SingleThreadWorker : IWorker, IDisposable
    {
        public event EventHandler<ExecutionFinishedEventArgs> ExecutionFinished;

        private RequestScheduler _serialRequestScheduler;
        private TaskFactory _serialTaskFactory;
        private bool _isDisposed;

        // Set of executing request identifiers mapped to it's action id
        private readonly ISet<int> _executingRequestIds;

        public SingleThreadWorker()
        {
            _executingRequestIds = new HashSet<int>();
            _serialRequestScheduler = new RequestScheduler(1);
            _serialTaskFactory = new TaskFactory(_serialRequestScheduler);
        }

        public void SubmitRequest(AbsRequest request)
        {
            _executingRequestIds.Add(request.RequestId);
            _serialTaskFactory.StartNew(() => request.Execute())
                .ContinueWith(task => FinishRequest(request));
        }

        public bool HasPendingRequests()
        {
            return _executingRequestIds.Count > 0;
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
                _serialRequestScheduler = null;
                _serialTaskFactory = null;
            }

            _isDisposed = true;
        }

        private void FinishRequest(AbsRequest request)
        {
            if (_isDisposed) return;

            _executingRequestIds.Remove(request.RequestId);

            if (HasPendingRequests() == false)
            {
                OnExecutionFinished();
            }
        }

        protected virtual void OnExecutionFinished()
        {
            if (ExecutionFinished != null)
            {
                ExecutionFinished(this, new ExecutionFinishedEventArgs());
            }
        }
    }
}