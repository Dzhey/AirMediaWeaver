using System;
using System.Collections.Generic;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Interfaces;
using AirMedia.Platform.Logger;
using Android.OS;
using Android.Util;
using Android.Widget;
using FragmentV4 = Android.Support.V4.App.Fragment;

namespace AirMedia.Platform.UI.Base
{
    public abstract class AmwFragment : FragmentV4
    {
        private const string ExtraRequestResultListenerState = "request_result_listener_state";
        private const string ExtraChildFragmentStates = "child_fragment_states";

        private string _logTag;

        private RequestResultListener _requestResultListener;
        private IDictionary<string, SavedState> _childrenStates;
        private List<IContextualRequestWorker> _registeredRequestWorkers;

        protected WorkerRequestManager WorkerRequestManager { get; private set; }

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

        protected RequestResultListener ResultListener
        {
            get { return _requestResultListener; }
        }

        public bool IsStarted { get; private set; }
        public bool IsResumed { get; private set; }

        public bool HasPendingRequests
        {
            get { return _requestResultListener.HasPendingRequests; }
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            int random = new Random().Next();

            _childrenStates = new Dictionary<string, SavedState>();

            WorkerRequestManager = App.WorkerRequestManager;
            _requestResultListener = new RequestResultListener(string.Format(
                "{0}_request_listener_{1}", GetType().Name, random));

            if (savedInstanceState != null)
            {
                var listenerState = savedInstanceState.GetBundle(ExtraRequestResultListenerState);
                _requestResultListener.RestoreInstanceState(listenerState);
                RestoreChildFragmentStates(savedInstanceState);
            }
        }

        public override void OnStart()
        {
            base.OnStart();

            _requestResultListener.ResultEventHandler += OnRequestResult;
            _requestResultListener.UpdateEventHandler += OnRequestUpdate;

            IsStarted = true;
        }

        public override void OnStop()
        {
            IsStarted = false;

            _requestResultListener.ResultEventHandler -= OnRequestResult;
            _requestResultListener.UpdateEventHandler -= OnRequestUpdate;

            base.OnStop();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            var state = _requestResultListener.SaveInstanceState();
            outState.PutBundle(ExtraRequestResultListenerState, state);

            if (_childrenStates.Count > 0)
            {
                var childrenStates = new Bundle();
                foreach (var entry in _childrenStates)
                {
                    childrenStates.PutParcelable(entry.Key, entry.Value);
                }

                outState.PutBundle(ExtraChildFragmentStates, childrenStates);
            }
        }


        public override void OnDestroy()
        {
            if (_registeredRequestWorkers != null)
            {
                foreach (var worker in _registeredRequestWorkers)
                {
                    worker.Dispose();
                }
                _registeredRequestWorkers.Clear();
            }

            _requestResultListener.Dispose();
            base.OnDestroy();
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
                throw new InvalidOperationException("only timemaster activtiy can display your message");
            }
            activity.ShowMessage(message, toastLength);
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

        public override void OnResume()
        {
            base.OnResume();

            InitRequestUpdateHandlers();
            InitRequestResultHandlers();

            IsResumed = true;
        }

        public override void OnPause()
        {
            IsResumed = false;

            ResetRequestUpdateHandlers();
            ResetRequestResultHandlers();

            base.OnPause();
        }

        protected virtual void OnRequestResult(AbsRequest request, ResultEventArgs args)
        {
        }

        protected virtual void OnRequestUpdate(AbsRequest request, UpdateEventArgs args)
        {
        }

        protected void RegisterRequestResultHandler(Type requestType, EventHandler<ResultEventArgs> handler)
        {
            _requestResultListener.RegisterResultHandler(requestType, handler);
        }

        protected void RegisterRequestUpdateHandler(Type requestType, EventHandler<UpdateEventArgs> handler)
        {
            _requestResultListener.RegisterUpdateHandler(requestType, handler);
        }

        protected void RemoveRequestResultHandler(Type requestType)
        {
            _requestResultListener.RemoveResultHandler(requestType);
        }

        protected void RemoveRequestUpdateHandler(Type requestType)
        {
            _requestResultListener.RemoveUpdateHandler(requestType);
        }

        /// <summary>
        /// Save the specified mapped to the key. 
        /// If provided state is null then stored state will be cleared.
        /// </summary>
        protected void SaveChildFragmentState(string key, SavedState state)
        {
            if (_childrenStates.ContainsKey(key))
            {
                _childrenStates.Remove(key);
            }

            if (state != null)
            {
                _childrenStates.Add(key, state);
            }
        }

        protected void RemoveChildFragmentState(string key)
        {
            if (_childrenStates.ContainsKey(key))
            {
                _childrenStates.Remove(key);
            }
        }

        protected SavedState GetChildFragmentState(string key)
        {
            if (_childrenStates.ContainsKey(key))
            {
                return _childrenStates[key];
            }

            return null;
        }

        protected void RegisterRequestWorker(IContextualRequestWorker worker)
        {
            if (worker == null)
                throw new ArgumentException("request worker should not be null");

            if (_registeredRequestWorkers == null)
            {
                _registeredRequestWorkers = new List<IContextualRequestWorker>();
            }

            _registeredRequestWorkers.Add(worker);
            if (IsStarted)
            {
                worker.InitUpdateHandler();
                worker.InitResultHandler();
            }
        }

        protected void RemoveRequestWorker(IContextualRequestWorker worker)
        {
            if (_registeredRequestWorkers == null || worker == null) return;

            if (_registeredRequestWorkers.Remove(worker))
            {
                worker.ResetUpdateHandler();
                worker.ResetResultHandler();
            }

            worker.Dispose();
        }

        /// <summary>
        /// Set initial state for specified fragment.
        /// State should be previously stored for specified key.
        /// if no saved state is available then nothing changed.
        /// </summary>
        /// <param name="fragment"></param>
        /// <param name="key"></param>
        protected void RestoreChildFragmentState(FragmentV4 fragment, string key)
        {
            var state = GetChildFragmentState(key);

            if (state != null)
            {
                fragment.SetInitialSavedState(state);
            }
        }

        private void RestoreChildFragmentStates(Bundle savedInstanceState)
        {
            _childrenStates.Clear();

            if (savedInstanceState.ContainsKey(ExtraChildFragmentStates) == false) return;

            var childrenStates = savedInstanceState.GetBundle(ExtraChildFragmentStates);
            foreach (var key in childrenStates.KeySet())
            {
                var state = childrenStates.GetParcelable(key) as SavedState;

                if (state == null)
                {
                    Log.Error(LogTag, string.Format("can't restore child fragment state " +
                                                    "for key: \"{0}\". Inconsistent state types.", key));
                    continue;
                }

                _childrenStates.Add(key, state);
            }
        }

        private void OnRequestResult(object sender, ResultEventArgs args)
        {
            OnRequestResult((AbsRequest)sender, args);
        }

        private void OnRequestUpdate(object sender, UpdateEventArgs args)
        {
            OnRequestUpdate((AbsRequest)sender, args);
        }

        private void InitRequestResultHandlers()
        {
            if (_registeredRequestWorkers == null) return;

            foreach (var worker in _registeredRequestWorkers)
            {
                worker.InitResultHandler();
            }
        }

        private void ResetRequestResultHandlers()
        {
            if (_registeredRequestWorkers == null) return;

            foreach (var worker in _registeredRequestWorkers)
            {
                worker.ResetResultHandler();
            }
        }

        private void InitRequestUpdateHandlers()
        {
            if (_registeredRequestWorkers == null) return;

            foreach (var worker in _registeredRequestWorkers)
            {
                worker.InitUpdateHandler();
            }
        }

        private void ResetRequestUpdateHandlers()
        {
            if (_registeredRequestWorkers == null) return;

            foreach (var worker in _registeredRequestWorkers)
            {
                worker.ResetUpdateHandler();
            }
        }
    }
}