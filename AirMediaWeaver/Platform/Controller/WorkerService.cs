using System;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace AirMedia.Platform.Controller
{
    [Service(Exported = false, Label = "AirMediaService")]
    class WorkerService : Service
    {
        private const int MaxDegreeOfParallelism = 4;

        private WorkerRequestManager _requestManager;
        private SingleThreadWorker _singleThreadedWorker;
        private ThreadPoolWorker _threadPoolWorker;

        public const string ActionProcessRequest = "process_request";
        public const string ExtraRequestId = "request_id";
        public const string ExtraIsParallelRequest = "is_parallel_request";
        public const string ExtraIsDedicatedRequest = "is_dedicated_request";

        public static readonly string LogTag = typeof(WorkerService).Name;

        public static WorkerService Instance { get; private set; }


        public override void OnCreate()
        {
            base.OnCreate();

            _singleThreadedWorker = new SingleThreadWorker();
            _threadPoolWorker = new AndroidThreadPoolWorker(MaxDegreeOfParallelism);
            _requestManager = App.WorkerRequestManager;

            _threadPoolWorker.ExecutionFinished += OnExecutionFinished;
            _singleThreadedWorker.ExecutionFinished += OnExecutionFinished;

            Instance = this;
            Log.Verbose(LogTag, "WorkerService created");
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
                    AmwLog.Error(LogTag, "Request (id:{0}) not found", requestId);

                    if (!HasPendingRequests()) StopSelf();
                }
                else
                {
                    bool isParallelRequest = intent.GetBooleanExtra(ExtraIsParallelRequest, false);
                    bool isDedicatedRequest = intent.GetBooleanExtra(ExtraIsDedicatedRequest, false);
                    SubmitRequest(request, isParallelRequest, isDedicatedRequest);
                }
            }

			return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            Instance = null;
            _threadPoolWorker.ExecutionFinished -= OnExecutionFinished;
            _singleThreadedWorker.ExecutionFinished -= OnExecutionFinished;
            base.OnDestroy();
            Log.Verbose(LogTag, "WorkerService destroyed");
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public void SubmitRequest(AbsRequest request, bool isParallel, bool isDedicated)
        {
            if (isDedicated)
            {
                _threadPoolWorker.SubmitDedicatedRequest(request);
                return;
            }

            if (isParallel)
            {
                _threadPoolWorker.SubmitRequest(request);
            }
            else
            {
                _singleThreadedWorker.SubmitRequest(request);
            }
        }

        public bool HasPendingRequests()
        {
            return _singleThreadedWorker.HasPendingRequests() 
                && _threadPoolWorker.HasPendingRequests();
        }

        private void OnExecutionFinished(object sender, ExecutionFinishedEventArgs args)
        {
            if (HasPendingRequests() == false) 
                StopSelf();
        }
    }
}