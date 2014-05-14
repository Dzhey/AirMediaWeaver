using System;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library;
using AirMedia.Platform.UI.Player;
using AirMedia.Platform.UI.Playlists;
using Android.App;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Widget;

namespace AirMedia.Platform.UI.MainView
{
    [Activity(Label = "Air Media", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainViewActivity : AmwActivity
    {
        private const string ExtraFragmentStateBundle = "fragment_state_bundle";
        private const string ExtraDisplayFragment = "display_fragment";
        private const string ContentFragmentTag = "content_fragment_tag";
        private const string PlayerFacadeFragmentTag = "player_facade_fragment_tag";

        private static readonly DrawerNavigationItem[] NavigationItems;

        private Bundle _fragmentStateBundle;
        private DrawerListAdapter _drawerListAdapter;
        private DrawerLayout _drawerLayout;
        private ListView _drawerList;

        static MainViewActivity()
        {
            NavigationItems = new[]
                {
                    new DrawerNavigationItem
                        {
                            StringResourceId = Resource.String.title_navigation_item_media_library
                        },
                    new DrawerNavigationItem
                        {
                            StringResourceId = Resource.String.title_navigation_item_playlists
                        }
                };
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.Activity_MainView);

            _drawerListAdapter = new DrawerListAdapter();
            _drawerListAdapter.SetItems(NavigationItems);
            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawerLayout);
            _drawerList = FindViewById<ListView>(Resource.Id.leftDrawer);
            _drawerList.Adapter = _drawerListAdapter;

            Type displayFragmentType = typeof(AudioLibraryFragment);
            if (bundle != null)
            {
                if (bundle.ContainsKey(ExtraFragmentStateBundle))
                {
                    _fragmentStateBundle = bundle.GetBundle(ExtraFragmentStateBundle);
                }

                if (bundle.ContainsKey(ExtraDisplayFragment))
                {
                    displayFragmentType = Type.GetType(bundle.GetString(ExtraDisplayFragment));
                }
            }

            SetContentFragment(displayFragmentType);

            var playerFacadeFragment = (PlayerFacadeFragment) FragmentManager.FindFragmentByTag(PlayerFacadeFragmentTag);

            if (playerFacadeFragment == null)
            {
                playerFacadeFragment = new PlayerFacadeFragment();
                FragmentManager.BeginTransaction()
                               .Add(Resource.Id.playerView, playerFacadeFragment, PlayerFacadeFragmentTag)
                               .Commit();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            _drawerList.ItemClick += OnNavigationItemClicked;
        }

        protected override void OnPause()
        {
            _drawerList.ItemClick -= OnNavigationItemClicked;

            base.OnPause();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutBundle(ExtraFragmentStateBundle, _fragmentStateBundle);

            var displayFragment = GetCurrentContentFragment();
            if (displayFragment != null)
            {
                string typeName = displayFragment.GetType().FullName;
                outState.PutString(ExtraDisplayFragment, typeName);
            }
        }

        private void OnNavigationItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            _drawerLayout.CloseDrawer(_drawerList);

            Type contentFragment = null;
            var item = _drawerListAdapter[args.Position];
            switch (item.StringResourceId)
            {
                case Resource.String.title_navigation_item_media_library:
                    contentFragment = typeof (AudioLibraryFragment);
                    break;

                case Resource.String.title_navigation_item_playlists:
                    contentFragment = typeof(PlaylistsViewFragment);
                    break;

                default:
                    AmwLog.Error(LogTag, "undefined navigation item clicked");
                    break;
            }

            if (contentFragment != null)
            {
                SetContentFragment(contentFragment);
            }
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

            string details = savedState == null
                                 ? "returned saved state is null"
                                 : string.Format("saved state: \"{0}\"; type: \"{1}\"", savedState,
                                                 savedState.GetType().FullName);
            AmwLog.Error(LogTag, string.Format(
                "Error retrieving fragment's state for fragment \"{0}\"", fragmentType.Name), 
                details);

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

