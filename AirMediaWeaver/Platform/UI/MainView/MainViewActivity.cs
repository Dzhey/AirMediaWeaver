using AirMedia.Platform.UI.Base;
using Android.App;
using Android.OS;

namespace AirMedia.Platform.UI.MainView
{
    [Activity(Label = "AirMediaWeaver", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainViewActivity : AmwActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.View_FragmentContainer);
        }
    }
}

