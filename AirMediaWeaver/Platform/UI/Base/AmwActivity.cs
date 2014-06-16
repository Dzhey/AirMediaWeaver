using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Logger;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Base
{
    public abstract class AmwActivity : FragmentActivity
    {
        private string _logTag;

        private const string ExtraRequestResultListenerState = "request_result_listener_state";
        private const string TagLogPanelFragment = "log_panel_fragment";

        private RequestResultListener _requestResultListener;
        private bool _isLogPanelDisplayed;

        protected WorkerRequestManager WorkerRequestManager { get; private set; }
        protected System.EventHandler NavigationBarSet { get; set; }

        protected string LogTag
        {
            get
            {
                if (_logTag == null)
                {
                    _logTag = GetType().Name;
                }

                return _logTag;
            }
        }

        public void UpdateLogPanel()
        {
            if (Core.Consts.IsInAppLoggingEnabled && App.Preferences.IsLogPanelEnabled)
            {
                DisplayLogPanel();
            }
            else
            {
                HideLogPanel();
            }
        }

        public void ShowMessage(int stringResourceId, ToastLength toastLength = ToastLength.Short)
        {
            ShowMessage(GetString(stringResourceId));
        }

        public void ShowMessage(string message, ToastLength toastLength = ToastLength.Short)
        {
            Toast.MakeText(this, message, toastLength).Show();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WorkerRequestManager = App.WorkerRequestManager;

            int random = new System.Random().Next();
            _requestResultListener = new RequestResultListener(string.Format("{0}_{1}", GetType().Name, random));

            if (savedInstanceState != null)
            {
                var listenerState = savedInstanceState.GetBundle(ExtraRequestResultListenerState);
                _requestResultListener.RestoreInstanceState(listenerState);
            }
        }

        protected override void OnStart()
        {
            UpdateLogPanel();

            base.OnStart();

            _requestResultListener.ResultEventHandler += OnRequestResult;
            _requestResultListener.UpdateEventHandler += OnRequestUpdate;
        }

        protected override void OnStop()
        {
            _requestResultListener.ResultEventHandler -= OnRequestResult;
            _requestResultListener.UpdateEventHandler -= OnRequestUpdate;

            base.OnStop();
        }

        protected override void OnDestroy()
        {
            _requestResultListener.Dispose();
            base.OnDestroy();
        }

        protected virtual void HideLogPanel()
        {
            if (_isLogPanelDisplayed == false) return;

            var fragment = SupportFragmentManager.FindFragmentByTag(TagLogPanelFragment) as InAppLoggingPanelFragment;
            if (fragment != null)
            {
                SupportFragmentManager.BeginTransaction().Remove(fragment).CommitAllowingStateLoss();
            }

            _isLogPanelDisplayed = false;
        }

        protected virtual void DisplayLogPanel()
        {
            if (_isLogPanelDisplayed) return;

            var container = FindViewById<ViewGroup>(Resource.Id.logPanelContainer);

            if (container != null)
            {
                var fragment = SupportFragmentManager.FindFragmentByTag(TagLogPanelFragment) as InAppLoggingPanelFragment;
                if (fragment == null)
                {
                    fragment = new InAppLoggingPanelFragment();
                    SupportFragmentManager.BeginTransaction()
                                          .Add(Resource.Id.logPanelContainer, fragment, TagLogPanelFragment)
                                          .CommitAllowingStateLoss();
                }
            }

            _isLogPanelDisplayed = true;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            var state = _requestResultListener.SaveInstanceState();
            outState.PutBundle(ExtraRequestResultListenerState, state);
        }

        /// <summary>
        /// Submit specified request to execution.
        /// </summary>
        /// <param name="request">Request specification to execute</param>
        /// <returns>generated request id</returns>
        public int SubmitRequest(AbsRequest request)
        {
            return _requestResultListener.SubmitRequest(request);
        }

        /// <summary>
        /// Submit request to parallel execution.
        /// Set request action tag to execute request in sequential order 
        /// with other requests sharing the same action tag.
        /// </summary>
        /// <param name="request">Request specification to execute</param>
        /// <returns>generated request id</returns>
        public int SubmitParallelRequest(AbsRequest request)
        {
            return _requestResultListener.SubmitRequest(request, true);
        }

        protected virtual void OnRequestResult(AbsRequest request, ResultEventArgs args)
        {
        }

        protected virtual void OnRequestUpdate(AbsRequest request, UpdateEventArgs args)
        {
        }

        protected void RegisterRequestResultHandler(System.Type requestType,
            System.EventHandler<ResultEventArgs> handler)
        {
            _requestResultListener.RegisterResultHandler(requestType, handler);
        }

        protected void RegisterRequestUpdateHandler(System.Type requestType,
            System.EventHandler<UpdateEventArgs> handler)
        {
            _requestResultListener.RegisterUpdateHandler(requestType, handler);
        }

        protected void RemoveRequestResultHandler(System.Type requestType)
        {
            _requestResultListener.RemoveResultHandler(requestType);
        }

        protected void RemoveRequestUpdateHandler(System.Type requestType)
        {
            _requestResultListener.RemoveUpdateHandler(requestType);
        }

        protected void SetHandleAllRequests()
        {
            _requestResultListener.ShouldHandleAllRequests = true;
        }

        private void OnRequestResult(object sender, ResultEventArgs args)
        {
            OnRequestResult((AbsRequest)sender, args);
        }

        private void OnRequestUpdate(object sender, UpdateEventArgs args)
        {
            OnRequestUpdate((AbsRequest)sender, args);
        }
    }
}