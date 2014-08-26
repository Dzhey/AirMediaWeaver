using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Interfaces;
using AirMedia.Platform.Logger;
using Android.OS;

namespace AirMedia.Platform.UI.Base
{
    public abstract class BaseContextualRequestWorker : IContextualRequestWorker
    {
        public static readonly string LogTag = typeof (BaseContextualRequestWorker).Name;

        public bool IsResultHandlerDisabled { get; set; }

        protected IContextualWorkerCallbacks Callbacks { get { return _callbacks; } }

        private const string ExtraResultListenerState = "result_listener_state";

        private readonly RequestResultListener _resultListener;
        private readonly Lazy<AndroidRequestFactory> _requestFactoryLazy;
        private AndroidRequestFactory RequestFactory { get { return _requestFactoryLazy.Value; } }
        private bool _isDisposed;
        private readonly IContextualWorkerCallbacks _callbacks;

        protected BaseContextualRequestWorker()
        {
            _resultListener = new RequestResultListener(LogTag);
            _requestFactoryLazy = new Lazy<AndroidRequestFactory>(GetRequestFactory);
        }

        protected BaseContextualRequestWorker(IContextualWorkerCallbacks callbacks) : this()
        {
            _callbacks = callbacks;
        }

        public void InitResultHandler()
        {
            _resultListener.RegisterResultHandler(RequestFactory.RequestType, OnRequestResult);
        }

        public void InitUpdateHandler()
        {
            _resultListener.RegisterUpdateHandler(RequestFactory.RequestType, OnRequestUpdate);
        }

        public void ResetResultHandler()
        {
            _resultListener.RemoveResultHandler(RequestFactory.RequestType);
        }

        public void ResetUpdateHandler()
        {
            _resultListener.RemoveUpdateHandler(RequestFactory.RequestType);
        }

        public AbsRequest PerformRequest()
        {
            if (IsResultHandlerDisabled) return null;

            var args = GetRequestArgs();
            if (args == null)
            {
                return RequestFactory.Submit();
            }
            
            return RequestFactory.Submit(args);
        }

        public Bundle SaveState()
        {
            var bundle = new Bundle();
            bundle.PutBundle(ExtraResultListenerState, _resultListener.SaveInstanceState());

            return bundle;
        }

        public void RestoreState(Bundle savedState)
        {
            var bundle = savedState.GetBundle(ExtraResultListenerState);
            if (bundle != null)
            {
                _resultListener.RestoreInstanceState(bundle);
            }
        }

        protected abstract AndroidRequestFactory CreateRequestFactory(RequestResultListener listener);

        protected virtual object[] GetRequestArgs()
        {
            return null;
        }

        protected virtual void OnRequestResultImpl(object sender, ResultEventArgs args)
        {
        }

        protected virtual void OnRequestUpdateImpl(object sender, UpdateEventArgs args)
        {
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
                if (RequestFactory != null)
                {
                    _resultListener.Dispose();
                    RequestFactory.Dispose();
                }
            }

            _isDisposed = true;
        }


        private void OnRequestResult(object sender, ResultEventArgs args)
        {
            if (IsResultHandlerDisabled) return;

            OnRequestResultImpl(sender, args);

            if (Callbacks != null)
            {
                Callbacks.OnWorkerRequestFinished(args);
            }
        }

        private void OnRequestUpdate(object sender, UpdateEventArgs args)
        {
            if (IsResultHandlerDisabled) return;

            OnRequestUpdateImpl(sender, args);

            if (Callbacks != null)
            {
                Callbacks.OnWorkerRequestUpdate(args);
            }
        }

        private AndroidRequestFactory GetRequestFactory()
        {
            return CreateRequestFactory(_resultListener);
        }
    }
}