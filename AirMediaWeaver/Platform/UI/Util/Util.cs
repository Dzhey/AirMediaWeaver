namespace AirMedia.Platform.UI.Util
{
    public static class Util
    {
        /*// Used to create styled toast messages
        public static Toast CreateToast(Context context, int stringResourceId, 
            ToastLength toastLength = ToastLength.Short)
        {
            return CreateToast(context, context.GetString(stringResourceId), toastLength);
        }

        public static Toast CreateToast(Context context, string message, 
            ToastLength toastLength = ToastLength.Short)
        {
            var view = (ViewGroup) LayoutInflater.From(context).Inflate(Resource.Layout.View_Toast, null);
            var messageView = view.FindViewById<TextView>(Resource.Id.message);
            messageView.Text = message;
            var width = context.Resources.DisplayMetrics.WidthPixels - 24;
            messageView.SetWidth(width);

            var toast = new Toast(context) { View = view, Duration = ToastLength.Short };
            toast.SetGravity(GravityFlags.Bottom | GravityFlags.CenterHorizontal, 0, 0);

            return toast;
        }*/
    }
}