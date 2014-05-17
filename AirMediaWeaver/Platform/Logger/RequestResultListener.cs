using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;
using Android.OS;
using Android.Util;
using Java.Security;

namespace AirMedia.Platform.Logger
{
    /// <summary>
    /// Provides convenient way to connect activiy, worker service and user requests.
    /// </summary>
    public class RequestResultListener : IRequestResultListener, IRequestUpdateListener, IDisposable
    {
        private static readonly string LogTag = typeof (RequestResultListener).Name;

        private const string ExtraRequestIds = "request_ids";
        private const string ExtraShouldHandleAllRequests = "should_handle_all_requests";

        private readonly ISet<int> _requestIds;
        private readonly IDictionary<Type, EventHandler<ResultEventArgs>> _resultDelegates; 
        private readonly IDictionary<Type, EventHandler<UpdateEventArgs>> _updateDelegates;
        private bool _isDisposed;

        public event EventHandler<UpdateEventArgs> UpdateEventHandler;
        public event EventHandler<ResultEventArgs> ResultEventHandler;

        public string Tag { get; private set; }

        public bool ShouldHandleAllRequests { get; set; }
        public int PendingRequestCount
        {
            get
            {
                lock (_requestIds)
                {
                    return _requestIds.Count;
                }
            }
        }
        public bool HasPendingRequests
        {
            get { return PendingRequestCount > 0; }
        }

        public RequestResultListener(string tag)
        {
            Tag = tag;
            _requestIds = new HashSet<int>();
            _resultDelegates = new Dictionary<Type, EventHandler<ResultEventArgs>>();
            _updateDelegates = new Dictionary<Type, EventHandler<UpdateEventArgs>>();
            App.WorkerRequestManager.RegisterEventHandler((IRequestResultListener) this);
            App.WorkerRequestManager.RegisterEventHandler((IRequestUpdateListener) this);
        }

        public override string ToString()
        {
            return string.Format("[RequestResultListener: {0}]", Tag);
        }

        public void RegisterResultHandler(Type requestType, EventHandler<ResultEventArgs> handler)
        {
            if (typeof (AbsRequest).IsAssignableFrom(requestType) == false)
            {
                throw new InvalidParameterException("specified type is not a request type");
            }

            if (_resultDelegates.ContainsKey(requestType))
            {
                throw new InvalidParameterException("specified type is already registered");
            }

            _resultDelegates.Add(requestType, handler);

            DeliverPendingResults();
        }

        public void RegisterUpdateHandler(Type requestType, EventHandler<UpdateEventArgs> handler)
        {
            if (typeof(AbsRequest).IsAssignableFrom(requestType) == false)
            {
                throw new InvalidParameterException("specified type is not a request type");
            }

            if (_updateDelegates.ContainsKey(requestType))
            {
                throw new InvalidParameterException("specified type is already registered");
            }

            _updateDelegates.Add(requestType, handler);
        }

        public void RemoveResultHandler(Type requestType)
        {
            _resultDelegates.Remove(requestType);
        }

        public void RemoveUpdateHandler(Type requestType)
        {
            _updateDelegates.Remove(requestType);
        }

        public int SubmitDedicatedRequest(AbsRequest request)
        {
            App.WorkerRequestManager.SubmitRequest(request, false, true);
            RegisterRequest(request.RequestId);

            return request.RequestId;
        }

        public int SubmitRequest(AbsRequest request, bool isParallel = false)
        {
            App.WorkerRequestManager.SubmitRequest(request, isParallel);
            RegisterRequest(request.RequestId);

            return request.RequestId;
        }

		public void CancelRequest(int requestId)
		{
			if (!HasPendingRequest (requestId))
				return;
			var request = App.WorkerRequestManager.FindRequest (requestId);
			if (request != null) 
			{
				request.Cancel ();
			}
		}

        public bool HasPendingRequest(int requestId)
        {
            lock (_requestIds)
            {
                return _requestIds.Contains(requestId);
            }
        }

        public void AddPendingRequest(int requestId)
        {
            RegisterRequest(requestId);
        }

        /// <summary>
        /// Used to deliver all pending request results to it's listeners.
        /// </summary>
        public void DeliverPendingResults()
        {
			int[] ids;
            lock (_requestIds)
            {
                ids = _requestIds.ToArray();    
            }
            
            foreach (int id in ids)
            {
                var request = App.WorkerRequestManager.FindRequest(id);
                if (request != null)
                {
                    if (request.IsFinished)
                    {
                        HandleRequestResult(request, request.GetRequestResultEventArgs());
                    }
                }
            } 
        }

        public void RestoreInstanceState(Bundle savedInstanceState)
        {
            if (savedInstanceState == null) return;

            var ids = savedInstanceState.GetIntArray(ExtraRequestIds);

            lock (_requestIds)
            {
                _requestIds.Clear();
                _requestIds.UnionWith(ids);
            }
            ShouldHandleAllRequests = savedInstanceState.GetBoolean(ExtraShouldHandleAllRequests, false);
            // Relieve pending requests
            DeliverPendingResults();
        }

        public Bundle SaveInstanceState()
        {
            var result = new Bundle();

            lock (_requestIds)
            {
                result.PutIntArray(ExtraRequestIds, _requestIds.ToArray());
            }
            result.PutBoolean(ExtraShouldHandleAllRequests, ShouldHandleAllRequests);

            return result;
        }

        public void HandleRequestResult(object sender, ResultEventArgs args)
        {
            int requestId = args.Request.RequestId;
            bool isPending = ShouldHandleAllRequests || IsPending(requestId);

            bool isHandled;
            if (isPending)
            {
                isHandled = DispatchRequestResult(sender, args, requestId);

                if (!isHandled)
                {
                    App.MainHandler.Post(() =>
                    {
                        if (ResultEventHandler != null)
                        {
                            RemoveRequest(requestId);
                            ResultEventHandler(sender, args);
                        }
                    });
                }
            }

        }

        public void HandleRequestUpdate(object sender, UpdateEventArgs args)
        {
            if (ShouldHandleAllRequests || IsPending(args.Request.RequestId))
            {
                if (DispatchRequestUpdate(sender, args)) return;

                if (UpdateEventHandler == null) return;

                App.MainHandler.Post(() =>
                {
                    if (UpdateEventHandler != null)
                    {
                        UpdateEventHandler(sender, args);
                    }
                });
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
                _resultDelegates.Clear();
                _updateDelegates.Clear();
                ResultEventHandler = null;
                UpdateEventHandler = null;
                App.WorkerRequestManager.RemoveEventHandler((IRequestResultListener)this);
                App.WorkerRequestManager.RemoveEventHandler((IRequestUpdateListener)this);

                lock (_requestIds)
                {
                    _requestIds.Clear();
                }
            }

            _isDisposed = true;
        }

        public bool IsPending(int? requestId)
        {
            if (requestId == null) return false;

            lock (_requestIds)
            {
                return _requestIds.Contains(requestId.Value);
            }
        }

		private bool DispatchRequestResult(object sender, ResultEventArgs args, int requestId)
        {
            EventHandler<ResultEventArgs> handler;
            bool result = _resultDelegates.TryGetValue(args.Request.GetType(), out handler);

            if (!result) return false;

			RemoveRequest (requestId);
            App.MainHandler.Post(() => handler(sender, args));

            return true;
        }

        private bool DispatchRequestUpdate(object sender, UpdateEventArgs args)
        {
            EventHandler<UpdateEventArgs> handler;
            bool result = _updateDelegates.TryGetValue(args.Request.GetType(), out handler);

            if (!result) return false;

            App.MainHandler.Post(() => handler(sender, args));

            return true;
        }

        private void RegisterRequest(int requestId)
        {
            lock (_requestIds)
            {
                if (_requestIds.Contains(requestId))
                {
                    string message = string.Format("Such requestId \"{0}\" is already registered", requestId);
                    Log.Warn(LogTag, message);
                    throw new InvalidOperationException(message);
                }
                _requestIds.Add(requestId);
            }
        }

		private void RemoveRequest(int requestId)
		{
			lock (_requestIds)
			{
				_requestIds.Remove(requestId);
			}
		}
    }
    
}