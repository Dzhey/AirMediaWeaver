
using System;
using AirMedia.Core.Log;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Java.Security;

namespace AirMedia.Platform.UI.Base
{
    [Activity(Exported = false)]
    public class FragmentContentActivity : AmwActivity, IMainViewFragmentCallbacks
    {
        public const string ExtraDisplayFragmentType = "display_fragment_type";
        public const string ExtraFragmentArguments = "fragment_arguments";

        private const string TagContentFragment = "content_fragment";

        public static Intent CreateStartIntent(Context context, 
            Type displayFragmentType, Bundle fragmentArgs = null)
        {
            if (typeof (AmwFragment).IsAssignableFrom(displayFragmentType) == false)
            {
                throw new InvalidParameterException(string.Format(
                    "specified type \"{0}\" is not type of \"{1}\"", displayFragmentType, typeof(AmwFragment)));
            }

            var intent = new Intent(context, typeof(FragmentContentActivity));
            intent.PutExtra(ExtraDisplayFragmentType, displayFragmentType.FullName);

            if (fragmentArgs != null)
            {
                intent.PutExtra(ExtraFragmentArguments, fragmentArgs);
            }

            return intent;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.View_FragmentContainer);

            var displayFragment = GetContentFragment();

            if (displayFragment != null) return;

            string typeName = Intent.GetStringExtra(ExtraDisplayFragmentType);
            var fragmentType = Type.GetType(typeName, false, true);

            if (fragmentType == null)
            {
                AmwLog.Error(LogTag, string.Format("can't retrieve fragment for " +
                                                   "specified type \"{0}\"", typeName));
                Finish();
                return;
            }

            if (typeof (AmwFragment).IsAssignableFrom(fragmentType) == false)
            {
                AmwLog.Error(LogTag, string.Format("invalid fragment type specified \"{0}\"", typeName));
                Finish();
                return;
            }

            displayFragment = (AmwFragment) Activator.CreateInstance(fragmentType);

            if (Intent.HasExtra(ExtraFragmentArguments))
            {
                displayFragment.Arguments = Intent.GetBundleExtra(ExtraFragmentArguments);
            }

            OverridePendingTransition(Resource.Animation.slide_in_right_to_left, 
                Resource.Animation.slide_out_right_to_left);

            FragmentManager.BeginTransaction()
                           .Add(Resource.Id.contentView, displayFragment, TagContentFragment)
                           .Commit();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            var fragment = GetContentFragment();

            if (fragment != null)
            {
                fragment.OnActivityResult(requestCode, resultCode, data);

                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public AmwFragment GetContentFragment()
        {
            return (AmwFragment) FragmentManager.FindFragmentByTag(TagContentFragment);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var fragment = GetContentFragment();

            if (fragment != null && fragment.OnOptionsItemSelected(item))
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void Finish()
        {
            base.Finish();

            OverridePendingTransition(Resource.Animation.slide_in_left_to_right,
                Resource.Animation.slide_out_left_to_right);
        }
    }
}