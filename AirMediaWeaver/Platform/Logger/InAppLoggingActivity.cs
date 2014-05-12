using AirMedia.Platform.UI;
using AirMedia.Platform.UI.Base;
using Android.App;
using Android.OS;

namespace AirMedia.Platform.Logger
{
    [Activity]
    public class InAppLoggingActivity : AmwActivity
    {
        private const string TagContentFragment = "inapplog_content_fragment";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.View_FragmentContainer);


            var fragment = FragmentManager.FindFragmentByTag(TagContentFragment);
            if (fragment == null)
            {
                fragment = new InAppLoggingFragment();
                FragmentManager.BeginTransaction()
                               .Add(Resource.Id.contentView, fragment, TagContentFragment)
                               .Commit();
            }
        }

        protected override void DisplayLogPanel()
        {
        }

        protected override void HideLogPanel()
        {
        }
    }
}