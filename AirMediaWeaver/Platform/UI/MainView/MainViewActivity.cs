using System;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library;
using AirMedia.Platform.UI.Player;
using Android.App;
using Android.OS;

namespace AirMedia.Platform.UI.MainView
{
    [Activity(Label = "Air Media", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainViewActivity : AmwActivity
    {
        private const string ExtraFragmentStateBundle = "fragment_state_bundle";
        private const string ContentFragmentTag = "content_fragment_tag";
        private const string PlayerFacadeFragmentTag = "player_facade_fragment_tag";

        private Bundle _fragmentStateBundle;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (bundle != null && bundle.ContainsKey(ExtraFragmentStateBundle))
            {
                _fragmentStateBundle = bundle.GetBundle(ExtraFragmentStateBundle);
            }
            
            SetContentView(Resource.Layout.Activity_MainView);

            SetContentFragment(typeof(AudioLibraryFragment));

            var playerFacadeFragment = (PlayerFacadeFragment) FragmentManager.FindFragmentByTag(PlayerFacadeFragmentTag);

            if (playerFacadeFragment == null)
            {
                playerFacadeFragment = new PlayerFacadeFragment();
                FragmentManager.BeginTransaction()
                               .Add(Resource.Id.playerView, playerFacadeFragment, PlayerFacadeFragmentTag)
                               .Commit();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutBundle(ExtraFragmentStateBundle, _fragmentStateBundle);
        }

        private void SetContentFragment(Type fragmentType)
        {
            if (typeof (AmwFragment).IsAssignableFrom(fragmentType) == false)
            {
                throw new ApplicationException(string.Format(
                    "requested invalid fragment type \"{0}\"", fragmentType.Name));
            }

            var currentFragment = GetCurrentContentFragment();

            if (currentFragment != null && currentFragment.GetType() == fragmentType) return;

            if (currentFragment != null) SaveFragmentState(currentFragment);

            string javaFragmentType = Java.Lang.Class.FromType(fragmentType).Name;
            currentFragment = (AmwFragment) Fragment.Instantiate(this, javaFragmentType);

            var savedState = GetSavedState(fragmentType);
            if (savedState != null)
            {
                currentFragment.SetInitialSavedState(savedState);
            }

            FragmentManager.BeginTransaction()
                           .Replace(Resource.Id.contentView, currentFragment, ContentFragmentTag)
                           .CommitAllowingStateLoss();
        }

        private AmwFragment GetCurrentContentFragment()
        {
            return FragmentManager.FindFragmentByTag(ContentFragmentTag) as AmwFragment;
        }

        private Fragment.SavedState GetSavedState(Type fragmentType)
        {
            if (_fragmentStateBundle == null) return null;

            var savedState = _fragmentStateBundle.GetParcelable(fragmentType.Name);
            var stateTyped = savedState as Fragment.SavedState;
            if (stateTyped != null)
            {
                return stateTyped;
            }
            
            AmwLog.Error(LogTag, string.Format(
                "Error retrieving fragment's state for fragment \"{0}\"", fragmentType.Name));

            return null;
        }

        private void SaveFragmentState(Fragment fragment)
        {
            if (_fragmentStateBundle == null) _fragmentStateBundle = new Bundle();

            try
            {
                var fragmentState = FragmentManager.SaveFragmentInstanceState(fragment);
                _fragmentStateBundle.PutParcelable(fragment.GetType().Name, fragmentState);
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, string.Format(
                    "Can't save fragment state for \"{0}\". " +
                    "Error: \"{1}\"", fragment.GetType().Name, e));
            }
        }
    }
}

