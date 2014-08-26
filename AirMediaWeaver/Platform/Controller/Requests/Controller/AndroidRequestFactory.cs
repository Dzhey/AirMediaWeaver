using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Platform.Logger;

namespace AirMedia.Platform.Controller.Requests.Controller
{
    public class AndroidRequestFactory : IRequestFactory
    {
        public event EventHandler<RequestSubmittedEventArgs> RequestSubmitted;

        public Type RequestType { get { return _requestFactory.RequestType; } }

        private RequestResultListener _listener;
        private RequestFactory _requestFactory;
        private bool _isDisposed;
        
        public static AndroidRequestFactory Init(RequestFactory factory, RequestResultListener listener)
        {
            return new AndroidRequestFactory(factory, listener);
        }

        protected AndroidRequestFactory(RequestFactory factory, RequestResultListener listener = null)
        {
            _listener = listener;
            _requestFactory = factory;
            _requestFactory.RequestSubmitted += OnRequestSubmitted;
        }

        private void OnRequestSubmitted(object sender, RequestSubmittedEventArgs args)
        {
            if (_listener != null && args.Request.HasRequestId)
            {
                _listener.AddPendingRequest(args.Request.RequestId);
            }

            if (RequestSubmitted != null)
                RequestSubmitted(this, args);
        }

        public AndroidRequestFactory SetListener(RequestResultListener listener)
        {
            _listener = listener;

            return this;
        }

        public AbsRequest Submit(params object[] args)
        {
            return _requestFactory.Submit(args);
        }

        public IRequestFactory SetActionTag(string actionTag)
        {
            _requestFactory.SetActionTag(actionTag);

            return this;
        }

        public IRequestFactory SetDedicated(bool isDedicated)
        {
            _requestFactory.SetDedicated(isDedicated);

            return this;
        }

        public IRequestFactory SetDistinct(bool isDistinct)
        {
            _requestFactory.SetDistinct(isDistinct);

            return this;
        }

        public IRequestFactory SetManager(RequestManager manager)
        {
            _requestFactory.SetManager(manager);

            return this;
        }

        public IRequestFactory SetParallel(bool isParallel)
        {
            _requestFactory.SetParallel(isParallel);

            return this;
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
                _requestFactory.RequestSubmitted -= OnRequestSubmitted;
                _requestFactory.Dispose();
                _requestFactory = null;
            }

            _isDisposed = true;
        }
    }
}