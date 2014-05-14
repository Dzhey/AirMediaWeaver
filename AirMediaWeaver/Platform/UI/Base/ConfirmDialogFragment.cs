using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.UI.Base
{
    public class ConfirmDialogFragment : BaseDialogFragment
	{
		private const string ExtraTitle = "title_dialog";
		private const string ExtraMessage = "message";
		private const string ExtraAcceptTitle = "title_accept";
		private const string ExtraDeclineTitle = "title_decline";
		private const string ExtraNeutralTitle = "title_neutral";
		private const string ExtraIconId = "icon_id";
        private const string ExtraDisplayCancel = "display_cancel";
		private const string ExtraPayload = "payload_bundle";

        /// <summary>
        /// Event fired with supplied payload bundle
        /// </summary>
        public event EventHandler<Bundle> AcceptClick;

        /// <summary>
        /// Event fired with supplied payload bundle
        /// </summary>
        public event EventHandler<Bundle> DeclineClick;

        /// <summary>
        /// Event fired with supplied payload bundle
        /// </summary>
        public event EventHandler<Bundle> NeutralClick;

	    public Bundle Payload
	    {
	        get
	        {
                if (Arguments == null) return null;

	            return Arguments.GetBundle(ExtraPayload);
	        }
	    }

        public static ConfirmDialogFragment NewInstance(
            Context context,
            string title, 
            string message = null, 
            Bundle payload = null, 
            string acceptText = null,
            string declineText = null, 
            string neutralButtonText = null, 
            int? iconId = null, 
            bool displayCancelButton = true,
            Type dialogType = null)
		{
			var args = new Bundle();
			args.PutString(ExtraTitle, title);
            args.PutString(ExtraMessage, message);
            args.PutString(ExtraAcceptTitle, acceptText);
            args.PutString(ExtraDeclineTitle, declineText);
            args.PutString(ExtraNeutralTitle, neutralButtonText);
            args.PutBundle(ExtraPayload, payload);
            args.PutBoolean(ExtraDisplayCancel, displayCancelButton);

            if (iconId != null)
            {
                args.PutInt(ExtraIconId, (int)iconId);
            }

            if (dialogType != null)
            {
                if (typeof (ConfirmDialogFragment).IsAssignableFrom(dialogType) == false)
                {
                    throw new ArgumentException(string.Format(
                        "Invalid dialog fragment type specified \"{0}\"", dialogType));
                }
            }
            else
            {
                dialogType = typeof(ConfirmDialogFragment);
            }

            var fname = Java.Lang.Class.FromType(dialogType).Name;

            return (ConfirmDialogFragment)Instantiate(context, fname, args);
		}

		public override Dialog OnCreateDialog(Bundle savedInstanceState)
		{
            var builder = new AlertDialog.Builder(Activity);

            BuildDialog(builder);

            return builder.Create();
		}

        protected virtual void BuildDialog(AlertDialog.Builder builder)
        {
            string title = Arguments.GetString(ExtraTitle);
            string message = Arguments.GetString(ExtraMessage);
            string acceptTitle = Arguments.GetString(ExtraAcceptTitle) ?? Activity.GetString(Android.Resource.String.Yes);
            string declineTitle = Arguments.GetString(ExtraDeclineTitle) ?? Activity.GetString(Android.Resource.String.Cancel);
            string neutralButtonTitle = Arguments.GetString(ExtraNeutralTitle);

            if (Arguments.ContainsKey(ExtraIconId))
            {
                builder.SetIcon(Arguments.GetInt(ExtraIconId));
            }

            builder.SetTitle(title);

            if (message != null)
            {
                builder.SetMessage(message);
            }

            builder.SetPositiveButton(acceptTitle, OnDialogButtonClicked);

            bool displayCancel = Arguments.GetBoolean(ExtraDisplayCancel);
            if (displayCancel)
            {
                builder.SetNegativeButton(declineTitle, OnDialogButtonClicked);
            }

            if (neutralButtonTitle != null)
            {
                builder.SetNeutralButton(neutralButtonTitle, OnDialogButtonClicked);
            }

            builder.SetCancelable(true);
        }

		protected override bool IsAppThemeApplied()
		{
			return true;
		}

        protected void PerformAccept()
        {
            if (AcceptClick != null) AcceptClick(this, Payload);
        }

        private void OnDialogButtonClicked(object sender, DialogClickEventArgs args)
        {
            var buttonType = (DialogButtonType)args.Which;

            switch (buttonType)
            {
                case DialogButtonType.Positive:
                    if (AcceptClick != null) AcceptClick(this, Payload);
                    break;

                case DialogButtonType.Negative:
                    if (DeclineClick != null) DeclineClick(this, Payload);
                    break;

                case DialogButtonType.Neutral:
                    if (NeutralClick != null) NeutralClick(this, Payload);
                    break;

                default:
                    throw new ApplicationException("undefined button clicked");
            }
        }
	}
}

