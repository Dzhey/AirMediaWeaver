using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;
using System;

namespace AirMedia.Core.Requests.Controller
{
    public abstract class RequestManager : IRequestManager, IDisposable
    {
        private static readonly string LogTag = typeof (RequestManager).Name;
        private static readonly object Mutex = new object();

        private const int RequestQueueSize = 60;
        private const int QueueFreeThreshold = 20;
        private const int ListenersDisposeThreshold = 100;

        private int _requestIdCounter;
        private readonly LinkedList<AbsRequest> _requestQueue;
        private readonly LinkedList<WeakReference<IRequestResultListener>> _resultEventHandlers;
        private readonly LinkedList<WeakReference<IRequestUpdateListener>> _updateEventHandlers;

        private bool _isDisposed;

        private static volatile RequestManager _instance;

        public static RequestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Mutex)
                    {
                        if (_instance == null)
                        {
                            throw new InvalidOperationException("RequestManager instance is not defined");
                        }       
                    }
                }

                return _instance;
            }

            private set
            {
                lock (Mutex)
                {
                    if (_instance != null)
                    {
                        throw new ApplicationException("RequestManager instance is already defined");
                    }

                    _instance = value;
                }
            }
        }

        public static void Init(RequestManager requestManagerInstance)
        {
            Instance = requestManagerInstance;
        }

        protected RequestManager()
        {
            _requestQueue = new LinkedList<AbsRequest>();
            _resultEventHandlers = new LinkedList<WeakReference<IRequestResultListener>>();
            _updateEventHandlers = new LinkedList<WeakReference<IRequestUpdateListener>>();
        }

        public void RegisterEventHandler(IRequestResultListener handler)
        {
            if (handler == null)
            {
                throw new ApplicationException("attempt to register null request result handler");
            }

            lock (_resultEventHandlers)
            {
                _resultEventHandlers.AddLast(new WeakReference<IRequestResultListener>(handler));
            }
        }

        public void RegisterEventHandler(IRequestUpdateListener handler)
        {
            if (handler == null)
            {
                throw new ApplicationException("attempt to register null request update handler");
            }

            lock (_updateEventHandlers)
            {
                _updateEventHandlers.AddLast(new WeakReference<IRequestUpdateListener>(handler));
            }
        }

        public void RemoveEventHandler(IRequestResultListener handler)
        {
            lock (_resultEventHandlers)
            {
                RemoveEventHandlerImpl(_resultEventHandlers, handler);
            }
        }

        public void RemoveEventHandler(IRequestUpdateListener handler)
        {
            lock (_updateEventHandlers)
            {
                RemoveEventHandlerImpl(_updateEventHandlers, handler);
            }
        }

        /// <summary>
        /// Cancel all requests with the specified tag.
        /// </summary>
        /// <param name="actionTag"></param>
        public void CancelAll(string actionTag)
        {
            if (actionTag == null) return;

            lock (_requestQueue)
            {
                foreach (var request in _requestQueue)
                {
                    if (request.ActionTag == actionTag)
                        request.Cancel();
                }
            }
        }

        public bool TryDisposeRequest(int requestId)
        {
            var request = FindRequest(requestId);

            return TryDisposeRequestImpl(request);
        }

        private bool TryDisposeRequestImpl(AbsRequest request)
        {
            if (request == null || request.IsFinished == false)
                return false;

            lock (_requestQueue)
            {
                _requestQueue.Remove(request);
            }

            request.Dispose();

            return true;
        }

        /// <summary>
        /// Find first request with the specified id.
        /// Request may have any status.
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>request instance, if any; null otherwise</returns>
        public AbsRequest FindRequest(int requestId)
        {
            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.RequestId == requestId);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>true if there any pending/progress request with specified id</returns>
        public bool HasPendingRequest(int requestId)
        {
            lock (_requestQueue)
            {
                var rq = FindRequest(requestId);

                return rq != null
                       && (rq.Status == RequestStatus.InProgress
                           || rq.Status == RequestStatus.Pending);
            }
        }

        /// <summary>
        /// Find first request with the specified tag.
        /// Request may have any status.
        /// </summary>
        /// <param name="actionTag"></param>
        /// <returns>request instance, if any; null otherwise</returns>
        public AbsRequest FindRequest(string actionTag)
        {
            if (actionTag == null) return null;

            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.ActionTag == actionTag);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="actionTag"></param>
        /// <returns>true if there any pending/progress request with specified tag</returns>
        public bool HasPendingRequest(string actionTag)
        {
            lock (_requestQueue)
            {
                var rq = FindRequest(actionTag);

                return rq != null
                       && (rq.Status == RequestStatus.InProgress
                           || rq.Status == RequestStatus.Pending);
            }
        }

        /// <summary>
        /// Perform request queue cleanup if queue size reached specified threshold.
        /// Only finished requests are removed.
        /// </summary>
        private void PerformRequestQueueCleanup()
        {
            if (_requestQueue.Count < RequestQueueSize + QueueFreeThreshold) return;

            lock (_requestQueue)
            {
                int freedCount = 0;
                var trash = _requestQueue.Take(QueueFreeThreshold).ToArray();
                foreach (var item in trash)
                {
                    if (item.IsFinished == false) continue;

                    item.UpdateEvent -= HandleRequestUpdate;
                    item.ResultEvent -= HandleRequestResult;
                    _requestQueue.Remove(item);
                    freedCount++;
                }

                if (freedCount < 1)
                {
                    AmwLog.Warn(LogTag, string.Format("can't free request queue; too many retaining " +
                                                        "requests; queue size: \"{0}\"", _requestQueue.Count));
                }
            }
        }
        
        public int SubmitRequest(AbsRequest request, bool isParallel = false, bool isDedicated = false)
        {
            int requestId = GenerateRequestId();
            request.RequestId = requestId;

            request.UpdateEvent += HandleRequestUpdate;
            request.ResultEvent += HandleRequestResult;

            lock (_requestQueue)
            {
                PerformRequestQueueCleanup();

                _requestQueue.AddLast(request);
            }

            SubmitRequestImpl(request, requestId, isParallel, isDedicated);

            return requestId;
        }

        /// <summary>
        /// SubmitRequest implementation.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestId"></param>
        /// <param name="isParallel"></param>
        /// <param name="isDedicated"></param>
        protected abstract void SubmitRequestImpl(AbsRequest request, int requestId, 
            bool isParallel, bool isDedicated);

        protected int GenerateRequestId()
        {
            return Interlocked.Increment(ref _requestIdCounter);
        }

		private void HandleRequestResult(object sender, ResultEventArgs args)
        {
            lock (_resultEventHandlers)
            {
                foreach (var resultRef in _resultEventHandlers.ToArray())
                {
                    IRequestResultListener handler;
                    if (resultRef.TryGetTarget(out handler) == false)
                    {
                        _resultEventHandlers.Remove(resultRef);
                    }
                    else if (handler != null)
                    {
                        handler.HandleRequestResult(sender, args);
                    }
                }
            }
		}

		private void HandleRequestUpdate(object sender, UpdateEventArgs args)
		{
            lock (_updateEventHandlers)
            {
                foreach (var updateRef in _updateEventHandlers.ToArray())
                {
                    IRequestUpdateListener handler;
                    if (updateRef.TryGetTarget(out handler) == false)
                    {
                        _updateEventHandlers.Remove(updateRef);
                    }
                    else if (handler != null)
                    {
                        handler.HandleRequestUpdate(sender, args);
                    }
                }
            }
		}

        private void RemoveEventHandlerImpl<T>(LinkedList<WeakReference<T>> pool, T handler) where T : class, IRequestListener
        {
            var enumerator = pool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var refHandler = enumerator.Current;

                if (refHandler == null)
                {
                    AmwLog.Warn(LogTag, "Unexpected null event handler found");
                    continue;
                }

                T value;
                if (refHandler.TryGetTarget(out value))
                {
                    if (ReferenceEquals(value, handler))
                    {
                        bool isRemoved = pool.Remove(refHandler);

                        if (!isRemoved)
                        {
                            AmwLog.Error(LogTag, "Can't remove event handler \"{0}\"", value);
                        }

                        break;
                    }
                }
            }

            DisposeDetachedListeners(_updateEventHandlers);
        }

        /// <summary>
        /// Remove weak references to disposed listeners.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        private void DisposeDetachedListeners<T>(LinkedList<WeakReference<T>> items) where T : class, IRequestListener
        {
            if (items.Count < ListenersDisposeThreshold) return;

            var copy = items.ToArray();
            items.Clear();
            int count = 0;
            foreach (var item in copy)
            {
                T outValue;
                if (item.TryGetTarget(out outValue))
                {
                    items.AddLast(item);
                }
                else
                {
                    count++;
                }
            }

            Debug.WriteLine(string.Format("{0} request listeners disposed", count), LogTag);
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
                _requestQueue.Clear();
                _resultEventHandlers.Clear();
                _updateEventHandlers.Clear();
            }

            _isDisposed = true;
        }
    }
}