using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Logger;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Widget;

namespace AirMedia.Platform.UI.Base
{
    public abstract class BaseDialogFragment : DialogFragment
    {
        private const string ExtraRequestResultListenerState = "request_result_listener_state";

        protected RequestResultListener RequestResultListener { get; private set; }
        protected WorkerRequestManager WorkerRequestManager { get; private set; }

        public event EventHandler CancelEvent;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            WorkerRequestManager = App.WorkerRequestManager;

            int random = new Random().Next();
            RequestResultListener = new RequestResultListener(string.Format("{0}_{1}", GetType().Name, random));
        }

        public override void OnStart()
        {
            base.OnStart();

            RequestResultListener.ResultEventHandler += OnRequestResult;
            RequestResultListener.UpdateEventHandler += OnRequestUpdate;
        }

        public override void OnStop()
        {
            RequestResultListener.ResultEventHandler -= OnRequestResult;
            RequestResultListener.UpdateEventHandler -= OnRequestUpdate;

            base.OnStop();
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            if (savedInstanceState != null)
            {
                var listenerState = savedInstanceState.GetBundle(ExtraRequestResultListenerState);
                RequestResultListener.RestoreInstanceState(listenerState);
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            var state = RequestResultListener.SaveInstanceState();
            outState.PutBundle(ExtraRequestResultListenerState, state);
        }

        public override void OnCancel(IDialogInterface dialogInterface)
        {
            base.OnCancel(dialogInterface);

            if (CancelEvent != null) CancelEvent(this, null);
        }

        public override void OnDestroy()
        {
            RequestResultListener.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        /// Submit specified request to execution.
        /// </summary>
        /// <param name="request">Request specification to execute</param>
        /// <returns>generated request id</returns>
        public int SubmitRequest(AbsRequest request)
        {
            return RequestResultListener.SubmitRequest(request);
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
			return RequestResultListener.SubmitRequest(request, true);
		}

        public void ShowMessage(int stringResourceId, ToastLength toastLength = ToastLength.Short)
        {
            ShowMessage(GetString(stringResourceId), toastLength);
        }

        public void ShowMessage(string message, ToastLength toastLength = ToastLength.Short)
        {
            if (Activity == null)
            {
                return;
            }
            var activity = Activity as AmwActivity;
            if (activity == null)
            {
                throw new InvalidOperationException("only base activtiy can display message this way");
            }
            activity.ShowMessage(message, toastLength);
        }

        protected virtual void OnRequestResult(AbsRequest request, ResultEventArgs args)
        {
        }

        protected virtual void OnRequestUpdate(AbsRequest request, UpdateEventArgs args)
        {
        }

        protected void RegisterRequestResultHandler(Type requestType, EventHandler<ResultEventArgs> handler)
        {
            RequestResultListener.RegisterResultHandler(requestType, handler);
        }

        protected void RegisterRequestUpdateHandler(Type requestType, EventHandler<UpdateEventArgs> handler)
        {
            RequestResultListener.RegisterUpdateHandler(requestType, handler);
        }

        protected void RemoveRequestResultHandler(Type requestType)
        {
            RequestResultListener.RemoveResultHandler(requestType);
        }

        protected void RemoveRequestUpdateHandler(Type requestType)
        {
            RequestResultListener.RemoveUpdateHandler(requestType);
        }

        protected virtual bool IsAppThemeApplied()
        {
            return true;
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