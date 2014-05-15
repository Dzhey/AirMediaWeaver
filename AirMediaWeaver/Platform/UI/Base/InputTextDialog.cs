
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace AirMedia.Platform.UI.Base
{
    public class InputTextDialogFragment : ConfirmDialogFragment
    {
        private const string ExtraInitialText = "initial_text";

        public string InputText
        {
            get
            {
                if (_inputView == null) return string.Empty;

                return _inputView.Text;
            }
        }

        private EditText _inputView;

        public static InputTextDialogFragment CreateInputDialog(
            Context context, 
            string title,
            Bundle payload = null,
            string acceptText = null,
            string declineText = null ,
            int? iconId = null,
            string initalText = null)
        {
            var dialog = (InputTextDialogFragment) NewInstance(context, title, null, payload, 
                acceptText, declineText, iconId: iconId, dialogType: typeof(InputTextDialogFragment));

            if (initalText != null)
            {
                dialog.Arguments.PutString(ExtraInitialText, initalText);
            }

            return dialog;
        }

        protected override void BuildDialog(AlertDialog.Builder builder)
        {
            base.BuildDialog(builder);

            var view = LayoutInflater.From(Activity).Inflate(Resource.Layout.View_InputField, null);

            _inputView = view.FindViewById<EditText>(Resource.Id.input);

            if (Arguments != null && Arguments.ContainsKey(ExtraInitialText))
            {
                _inputView.Text = Arguments.GetString(ExtraInitialText);
            }

            builder.SetView(view);
        }

        public override void OnResume()
        {
            base.OnResume();

            _inputView.EditorAction += OnEditorAction;
        }

        public override void OnPause()
        {
            _inputView.EditorAction -= OnEditorAction;

            base.OnPause();
        }

        protected virtual void OnEditorAction(object sender, TextView.EditorActionEventArgs args)
        {
            if (args.ActionId == ImeAction.Done)
            {
                PerformAccept();
                Dismiss();
            }
        }
    }
}