using System;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.UI.Base;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Publications
{
    public class PublicationsTabFragment : MainViewFragment, ActionBar.IOnNavigationListener
    {
        private const string ContentFragmentTag = "PublicationsTabFragment_content_fragment";

        private const int LanPublicationsIndex = 0;
        private const int MyPublicationsIndex = 1;

        public override void OnAttach(Activity activity)
        {
            base.OnAttach(activity);

            var items = Resources.GetStringArray(Resource.Array.publications_navigation_items);
            var adapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleSpinnerItem, items);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleDropDownItem1Line);

            Activity.ActionBar.NavigationMode = ActionBarNavigationMode.List;
            Activity.ActionBar.SetListNavigationCallbacks(adapter, this);
        }

        public override void OnDetach()
        {
            base.OnDetach();

            Activity.ActionBar.NavigationMode = ActionBarNavigationMode.Standard;
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.View_FragmentContainer, container, false);
        }

        public override string GetTitle()
        {
            return "";
        }

        public override void OnGenericPlaybackRequested()
        {
            var fragment = GetContentFragment();
            if (fragment != null)
            {
                fragment.OnGenericPlaybackRequested();
            }
        }

        public override bool HasDisplayedContent()
        {
            var fragment = GetContentFragment();
            if (fragment != null)
            {
                fragment.HasDisplayedContent();
            }

            return false;
        }

        public bool OnNavigationItemSelected(int itemPosition, long itemId)
        {
            switch (itemPosition)
            {
                case LanPublicationsIndex:
                    SetContentFragment(typeof(MyPublicationsFragment));
                    break;

                case MyPublicationsIndex:
                    SetContentFragment(typeof(MyPublicationsFragment));
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format(
                        "undefined navigation item selected: {0}", itemPosition));
                    break;
            }

            return true;
        }

        private void SetContentFragment(Type fragmentType)
        {
            if (typeof(MainViewFragment).IsAssignableFrom(fragmentType) == false)
            {
                throw new ApplicationException(string.Format(
                    "requested invalid fragment type \"{0}\"", fragmentType.Name));
            }

            var currentFragment = GetContentFragment();

            if (currentFragment != null && currentFragment.GetType() == fragmentType) return;

            if (currentFragment != null) SaveChildFragmentState(currentFragment);

            string javaFragmentType = Java.Lang.Class.FromType(fragmentType).Name;
            currentFragment = (MainViewFragment) Instantiate(Activity, javaFragmentType);

            var savedState = GetChildFragmentState(fragmentType);
            if (savedState != null)
            {
                currentFragment.SetInitialSavedState(savedState);
            }

            FragmentManager.BeginTransaction()
                           .Replace(Resource.Id.contentView, currentFragment, ContentFragmentTag)
                           .CommitAllowingStateLoss();

            App.Preferences.LastMainView = fragmentType;
        }

        private MainViewFragment GetContentFragment()
        {
            return FragmentManager.FindFragmentByTag(ContentFragmentTag) as MainViewFragment;
        }

        private void SaveChildFragmentState(MainViewFragment fragment)
        {
            var state = FragmentManager.SaveFragmentInstanceState(fragment);

            if (state == null) return;

            SaveChildFragmentState(fragment.GetType().FullName, state);
        }

        private SavedState GetChildFragmentState(Type fragmentType)
        {
            return GetChildFragmentState(fragmentType.FullName);
        }
    }
}