using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;
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
        private readonly IRequestManager _requestManager;
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

        public static RequestResultListener NewInstance(string tag, IRequestManager requestManager = null)
        {
            int random = new Random().Next(int.MaxValue);
            string listenerTag = string.Format("{0}_{1}", tag, random);

            return new RequestResultListener(listenerTag, requestManager);
        }

        public RequestResultListener(string tag, IRequestManager requestManager = null)
        {
            Tag = tag;
            _requestIds = new HashSet<int>();
            _resultDelegates = new Dictionary<Type, EventHandler<ResultEventArgs>>();
            _updateDelegates = new Dictionary<Type, EventHandler<UpdateEventArgs>>();
            _requestManager = requestManager ?? RequestManager.Instance;

            _requestManager.RegisterEventHandler((IRequestResultListener)this);
            _requestManager.RegisterEventHandler((IRequestUpdateListener)this);
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
                AmwLog.Warn(LogTag, "specified type is already registered", requestType.ToString());
                return;
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
                AmwLog.Warn(LogTag, "specified type is already registered", requestType.ToString());
                return;
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
            _requestManager.SubmitRequest(request, false, true);
            RegisterRequest(request.RequestId);

            return request.RequestId;
        }

        public int SubmitRequest(AbsRequest request, bool isParallel = false)
        {
            _requestManager.SubmitRequest(request, isParallel);
            RegisterRequest(request.RequestId);

            return request.RequestId;
        }

		public void CancelRequest(int requestId)
		{
			if (!HasPendingRequest (requestId))
				return;
            var request = _requestManager.FindRequest(requestId);
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
                var request = _requestManager.FindRequest(id);
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
                _requestManager.RemoveEventHandler((IRequestResultListener)this);
                _requestManager.RemoveEventHandler((IRequestUpdateListener)this);

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