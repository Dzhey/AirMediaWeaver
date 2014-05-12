using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;
using Android.App;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Controller
{
    [Service(Exported = false, Label = "AirMediaService")]
    class WorkerService : Service
    {
        private const int MaxDegreeOfParallelism = 4;
        private const string RequestActionIdDefault = "__default_action";

        // Set of executing request identifiers mapped to it's action id
        private IDictionary<string, ISet<int>> _executingRequestIds;

        // Mapping between request action id and queue of the requests sharing that action id
        // Only single parallel request with the same action id may execute at each moment
        // Requests with the null or default request action id has no parallel execution constraints
        private Dictionary<string, LinkedList<AbsRequest>> _queuedRequests;

        private WorkerRequestManager _requestManager;
        private RequestScheduler _serialRequestScheduler;
        private RequestScheduler _parallelRequestScheduler;
        private TaskFactory _serialTaskFactory;
        private TaskFactory _parallelTaskFactory;

        public const string ActionProcessRequest = "process_request";
        public const string ExtraRequestId = "request_id";
        public const string ExtraIsParallelRequest = "is_parallel_request";

        public static readonly string LogTag = typeof(WorkerService).Name;


        public override void OnCreate()
        {
            base.OnCreate();

            _queuedRequests = new Dictionary<string, LinkedList<AbsRequest>>();
            _parallelRequestScheduler = new RequestScheduler(MaxDegreeOfParallelism);
            _serialRequestScheduler = new RequestScheduler(1);
            _serialTaskFactory = new TaskFactory(_serialRequestScheduler);
            _parallelTaskFactory = new TaskFactory(_parallelRequestScheduler);
            _executingRequestIds = new Dictionary<string, ISet<int>>();
            _requestManager = App.WorkerRequestManager;

            AmwLog.Verbose(LogTag, "TimeMasterService created");
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            lock (this)
            {
                if ((flags & StartCommandFlags.Redelivery) == StartCommandFlags.Redelivery)
                {
                    AmwLog.Warn(LogTag, string.Format("intent \"{0}\" redelivered", 
                        intent.Action), intent.ToString());
                }

                if (intent.Action.Equals(ActionProcessRequest) == false)
                {
                    throw new InvalidProgramException(string.Format("unsupported action {0}", intent.Action));
                }

                if (intent.HasExtra(ExtraRequestId) == false)
                {
                    throw new InvalidOperationException("request-id should be specified to process request");
                }

                int requestId = intent.GetIntExtra(ExtraRequestId, -1);
                var request = _requestManager.FindRequest(requestId);

                if (request == null)
                {
                    AmwLog.Error(LogTag, string.Format("Request (id:{0}) not found", requestId));

                    if (!HasPendingRequests()) StopSelf();
                }
                else
                {
                    bool isParallelRequest = intent.GetBooleanExtra(ExtraIsParallelRequest, false);

                    if (isParallelRequest)
                    {
                        SubmitParallelRequest(request);
                    }
                    else
                    {
                        SubmitRequest(request);
                    }
                }
            }

			return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AmwLog.Verbose(LogTag, "AirMediaService destroyed");
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        private void SubmitRequest(AbsRequest request)
        {
            _serialTaskFactory.StartNew(() => request.Execute())
                .ContinueWith(task => App.MainHandler.Post(() => FinishRequest(request, false)));
        }

        private void SubmitParallelRequest(AbsRequest request)
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
                .ContinueWith(task => App.MainHandler.Post(() => FinishRequest(request, true)));
        }

        private void FinishRequest(AbsRequest request, bool isParallelRequest)
        {
            string actionId = request.ActionTag ?? RequestActionIdDefault;

            if (_executingRequestIds.ContainsKey(actionId))
            {
                _executingRequestIds[actionId].Remove(request.RequestId);
            }

            if (isParallelRequest)
            {
                var pendingRequest = DequeRequest(actionId);

                if (pendingRequest != null)
                {
                    SubmitParallelRequest(pendingRequest);
                }
            }

            if (_executingRequestIds.Count == 0)
            {
                StopSelf();
            }    
        }

        private bool HasPendingRequests()
        {
            return _executingRequestIds.Count > 0;
        }

        private bool HasPendingRequest(string actionId)
        {
            if (actionId == null) actionId = RequestActionIdDefault;

            if (_executingRequestIds.ContainsKey(actionId))
            {
                return _executingRequestIds[actionId].Count > 0;
            }

            return false;
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

        private void EnqueueRequest(AbsRequest request)
        {
            string actionId = request.ActionTag ?? RequestActionIdDefault;

            if (_queuedRequests.ContainsKey(actionId) == false)
            {
                _queuedRequests[actionId] = new LinkedList<AbsRequest>();
            }
            _queuedRequests[actionId].AddLast(request);
        }
    }
}