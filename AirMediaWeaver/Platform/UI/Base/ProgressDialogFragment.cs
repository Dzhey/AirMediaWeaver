using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using Android.App;
using Android.OS;

namespace AirMedia.Platform.UI.Base
{
    public class ProgressDialogFragment : BaseDialogFragment
    {
        private string _message;
        private int? _pendingRequestId;

        public static readonly string LogTag = typeof(ProgressDialogFragment).Name;
        public static readonly string ExtraMessage = "message";
        public static readonly string ExtraPendingRequestId = "pending_request_id";

        public event EventHandler FinishEvent;

        public int? PendingRequestId
        {
            get { return _pendingRequestId; }
        }

        public static ProgressDialogFragment NewInstance(string message = null, int? pendingRequestId = null)
        {
            var args = new Bundle();
            args.PutString(ExtraMessage, message);
            if (pendingRequestId != null)
            {
                args.PutInt(ExtraPendingRequestId, (int) pendingRequestId);
            }
            var fragment = new ProgressDialogFragment { Arguments = args };

            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var args = Arguments;

            if (args == null) return;

            if (args.ContainsKey(ExtraMessage))
            {
                _message = args.GetString(ExtraMessage);
            }

            if (args.ContainsKey(ExtraPendingRequestId))
            {
                int requestId = args.GetInt(ExtraPendingRequestId);
                if (RequestResultListener.HasPendingRequest(requestId) == false)
                {
                    RequestResultListener.AddPendingRequest(requestId);

                    var request = App.WorkerRequestManager.FindRequest(requestId);
                    if (request != null && request.IsFinished)
                    {
                        Finish();
                        return;
                    }
                }
                _pendingRequestId = requestId;
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = new ProgressDialog(Activity);
            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(false);
            dialog.SetTitle(_message);

            return dialog;
        }

        protected override void OnRequestResult(AbsRequest request, ResultEventArgs args)
        {
            if (request.RequestId == _pendingRequestId)
            {
                Finish();
            }
        }

        private void Finish()
        {
            Dismiss();
            if (FinishEvent != null)
            {
                FinishEvent(this, EventArgs.Empty);
            }
        }
    }
}