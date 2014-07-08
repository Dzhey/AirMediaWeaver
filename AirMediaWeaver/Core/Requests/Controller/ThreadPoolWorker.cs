using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Controller
{
    public abstract class ThreadPoolWorker : IWorker, IDisposable
    {
        public const int MaxDegreeOfParallelism = 4;
        private const string RequestActionIdDefault = "__default_action";

        // Set of executing request identifiers mapped to it's action id
        private readonly IDictionary<string, ISet<int>> _executingRequestIds;

        // Mapping between request action id and queue of the requests sharing that action id
        // Only single parallel request with the same action id may execute at each moment
        // Requests with the null or default request action id has no parallel execution constraints
        private readonly Dictionary<string, LinkedList<AbsRequest>> _queuedRequests;

        private RequestScheduler _parallelRequestScheduler;
        private TaskFactory _parallelTaskFactory;
        private bool _isDisposed;

        public event EventHandler<ExecutionFinishedEventArgs> ExecutionFinished;

        public ThreadPoolWorker(int threadPoolSize = MaxDegreeOfParallelism)
        {
            _executingRequestIds = new Dictionary<string, ISet<int>>();
            _queuedRequests = new Dictionary<string, LinkedList<AbsRequest>>();
            _parallelRequestScheduler = new RequestScheduler(threadPoolSize);
            _parallelTaskFactory = new TaskFactory(_parallelRequestScheduler);
        }

        /// <summary>
        /// Submit request on execution in dedicated thread.
        /// </summary>
        /// <param name="request"></param>
        public void SubmitDedicatedRequest(AbsRequest request)
        {
            string actionId = request.ActionTag ?? RequestActionIdDefault;

            if (_executingRequestIds.ContainsKey(actionId) == false)
            {
                _executingRequestIds[actionId] = new HashSet<int>();
            }
            _executingRequestIds[actionId].Add(request.RequestId);

            Task.Factory.StartNew(() => request.Execute())
                .ContinueWith(task => InvokeOnMainThread(() => FinishRequest(request, false)));
        }

        /// <summary>
        /// Submit request on parallel execution.
        /// </summary>
        /// <param name="request"></param>
        public void SubmitRequest(AbsRequest request)
        {
            string actionId = request.ActionTag ?? RequestActionIdDefault;
            if (actionId != RequestActionIdDefault)
            {
                if (HasPendingRequest(request.ActionTag))
                {
                    EnqueueRequest(request);

                    return;
                }
            }

            if (_executingRequestIds.ContainsKey(actionId) == false)
            {
                _executingRequestIds[actionId] = new HashSet<int>();
            }
            _executingRequestIds[actionId].Add(request.RequestId);

            _parallelTaskFactory.StartNew(() => request.Execute())
                .ContinueWith(task => InvokeOnMainThread(() => FinishRequest(request, true)));
        }

        public bool HasPendingRequests()
        {
            return _executingRequestIds.Count > 0;
        }

        public bool HasPendingRequest(string actionId)
        {
            if (actionId == null) actionId = RequestActionIdDefault;

            if (_executingRequestIds.ContainsKey(actionId))
            {
                return _executingRequestIds[actionId].Count > 0;
            }

            return false;
        }

        private void EnqueueRequest(AbsRequest request)
        {
            string actionId = request.ActionTag ?? RequestActionIdDefault;

            if (_queuedRequests.ContainsKey(actionId) == false)
            {
                _queuedRequests[actionId] = new LinkedList<AbsRequest>();
            }
            _queuedRequests[actionId].AddLast(request);
        }

        protected abstract void InvokeOnMainThread(Action action);

        private void FinishRequest(AbsRequest request, bool isParallelRequest)
        {
            if (_isDisposed) return;

            string actionId = request.ActionTag ?? RequestActionIdDefault;

            if (_executingRequestIds.ContainsKey(actionId))
            {
                var set = _executingRequestIds[actionId];
                set.Remove(request.RequestId);
                if (set.Count == 0)
                {
                    _executingRequestIds.Remove(actionId);
                }
            }

            if (isParallelRequest)
            {
                var pendingRequest = DequeRequest(actionId);

                if (pendingRequest != null)
                {
                    SubmitRequest(pendingRequest);
                }
            }

            if (_executingRequestIds.Count == 0)
            {
                OnExecutionFinished();
            }
        }

        private AbsRequest DequeRequest(string actionId)
        {
            if (actionId == null || actionId == RequestActionIdDefault)
            {
                return null;
            }

            if (_queuedRequests.ContainsKey(actionId))
            {
                var list = _queuedRequests[actionId];

                if (list.Count < 1) return null;

                var result = list.First.Value;
                list.RemoveFirst();

                return result;
            }

            return null;
        }

        protected virtual void OnExecutionFinished()
        {
            if (_isDisposed) return;

            if (ExecutionFinished != null)
            {
                ExecutionFinished(this, new ExecutionFinishedEventArgs());
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
                _parallelRequestScheduler = null;
                _parallelTaskFactory = null;
            }

            _isDisposed = true;
        }
    }
}