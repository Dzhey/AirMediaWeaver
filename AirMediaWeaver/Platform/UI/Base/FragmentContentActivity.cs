
using System;
using AirMedia.Core.Log;
using Android.App;
using Android.Content;
using Android.OS;
using Java.Security;

namespace AirMedia.Platform.UI.Base
{
    [Activity(Exported = false)]
    public class FragmentContentActivity : AmwActivity
    {
        public const string ExtraDisplayFragmentType = "display_fragment_type";
        public const string ExtraFragmentArguments = "fragment_arguments";

        private const string TagContentFragment = "content_fragment";

        public static void StartAcitvity(Context launchContext, 
            Type displayFragmentType, Bundle fragmentArgs = null)
        {
            if (typeof (AmwFragment).IsAssignableFrom(displayFragmentType) == false)
            {
                throw new InvalidParameterException(string.Format(
                    "specified type \"{0}\" is not type of \"{1}\"", displayFragmentType, typeof(AmwFragment)));
            }

            var intent = new Intent(launchContext, typeof(FragmentContentActivity));
            intent.PutExtra(ExtraDisplayFragmentType, displayFragmentType.FullName);

            if (fragmentArgs != null)
            {
                intent.PutExtra(ExtraFragmentArguments, fragmentArgs);
            }

            launchContext.StartActivity(intent);
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

            FragmentManager.BeginTransaction()
                           .Add(Resource.Id.contentView, displayFragment, TagContentFragment)
                           .Commit();
        }

        public AmwFragment GetContentFragment()
        {
            return (AmwFragment) FragmentManager.FindFragmentByTag(TagContentFragment);
        }
    }
}