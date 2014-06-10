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
    public abstract class RequestManager : IRequestManager
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

        private static RequestManager _instance;

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

        protected RequestManager()
        {
            Instance = this;

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

        public AbsRequest FindRequest(int requestId)
        {
            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.RequestId == requestId);
            }
        }

        public bool HasRequest(int requestId)
        {
            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.RequestId == requestId) != null;
            }
        }

        public AbsRequest FindRequest(string actionTag)
        {
            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.ActionTag == actionTag);
            }
        }

        public bool HasRequest(string actionTag)
        {
            lock (_requestQueue)
            {
                return _requestQueue.FirstOrDefault(request => request.ActionTag == actionTag) != null;
            }
        }

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
                    foreach (var item in trash)
                    {
                        AmwLog.Warn(LogTag, string.Format("rataining request: \"{0}\"", item));
                    }
                }
            }
        }

        /// <returns>generated request id</returns>
        public int SubmitRequest(AbsRequest request, bool isParallel = false, bool isDedicated = false)
        {
//            Debug.WriteLine(string.Format("request submitted: {0}; parallel: {1}", request, isParallel), LogTag);

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

        protected abstract void SubmitRequestImpl(AbsRequest request, int requestId, 
            bool isParallel, bool isDedicated);

        protected int GenerateRequestId()
        {
            return Interlocked.Increment(ref _requestIdCounter);
        }

		private void HandleRequestResult(object sender, ResultEventArgs args)
        {
            Debug.WriteLine(string.Format("handle request result: {0}", args.Request), LogTag);
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
    }
}