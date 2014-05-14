using System;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library;
using AirMedia.Platform.UI.Player;
using AirMedia.Platform.UI.Playlists;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Fragment = Android.App.Fragment;

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
        private MainViewDrawerToggle _drawerToggle;

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

            _drawerToggle = new MainViewDrawerToggle(this, _drawerLayout, Resource.Drawable.ic_drawer,
                                                     Resource.String.hint_open_main_drawer,
                                                     Resource.String.hint_close_main_drawer);
            
            _drawerLayout.SetDrawerListener(_drawerToggle);

            ActionBar.SetHomeButtonEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

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

            _drawerToggle.DrawerOpened += OnDrawerOpened;
            _drawerToggle.DrawerClosed += OnDrawerClosed;
            _drawerList.ItemClick += OnNavigationItemClicked;
        }

        protected override void OnPause()
        {
            _drawerToggle.DrawerOpened -= OnDrawerOpened;
            _drawerToggle.DrawerClosed -= OnDrawerClosed;
            _drawerList.ItemClick -= OnNavigationItemClicked;

            base.OnPause();
        }

        private void OnDrawerOpened(object sender, EventArgs args)
        {
            ActionBar.SetTitle(Resource.String.title_main_view);
        }

        private void OnDrawerClosed(object sender, EventArgs args)
        {
            var contentFragment = GetContentFragment();

            if (contentFragment != null)
            {
                ActionBar.Title = contentFragment.GetTitle();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutBundle(ExtraFragmentStateBundle, _fragmentStateBundle);

            var displayFragment = GetContentFragment();
            if (displayFragment != null)
            {
                string typeName = displayFragment.GetType().FullName;
                outState.PutString(ExtraDisplayFragment, typeName);
            }
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);

            _drawerToggle.SyncState();
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

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            _drawerToggle.OnConfigurationChanged(newConfig);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (_drawerToggle.OnOptionsItemSelected(item)) return true;

            return base.OnOptionsItemSelected(item);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Menu)
            {
                if (!_drawerLayout.IsDrawerOpen(_drawerList))
                {
                    _drawerLayout.OpenDrawer(_drawerList);
                }
                else
                {
                    _drawerLayout.CloseDrawer(_drawerList);
                }

                return true;
            }

            return base.OnKeyDown(keyCode, e);
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
                App.MainHandler.PostDelayed(() => SetContentFragment(contentFragment), 200);
            }
        }

        private void SetContentFragment(Type fragmentType)
        {
            if (typeof (MainViewFragment).IsAssignableFrom(fragmentType) == false)
            {
                throw new ApplicationException(string.Format(
                    "requested invalid fragment type \"{0}\"", fragmentType.Name));
            }

            var currentFragment = GetContentFragment();

            if (currentFragment != null && currentFragment.GetType() == fragmentType) return;

            if (currentFragment != null) SaveFragmentState(currentFragment);

            string javaFragmentType = Java.Lang.Class.FromType(fragmentType).Name;
            currentFragment = (MainViewFragment)Fragment.Instantiate(this, javaFragmentType);

            var savedState = GetSavedState(fragmentType);
            if (savedState != null)
            {
                currentFragment.SetInitialSavedState(savedState);
            }

            FragmentManager.BeginTransaction()
                           .Replace(Resource.Id.contentView, currentFragment, ContentFragmentTag)
                           .CommitAllowingStateLoss();
        }

        private MainViewFragment GetContentFragment()
        {
            return FragmentManager.FindFragmentByTag(ContentFragmentTag) as MainViewFragment;
        }

        private Fragment.SavedState GetSavedState(Type fragmentType)
        {
            if (_fragmentStateBundle == null) return null;

            var savedState = _fragmentStateBundle.GetParcelable(fragmentType.Name);

            if (savedState == null) return null;

            var stateTyped = savedState as Fragment.SavedState;
            if (stateTyped != null)
            {
                return stateTyped;
            }

            string details = string.Format("saved state: \"{0}\"; type: \"{1}\"", 
                savedState, savedState.GetType().FullName);

            AmwLog.Error(LogTag, string.Format(
                "Error retrieving fragment's state for fragment \"{0}\"", fragmentType.Name), 
                details);

            return null;
        }

        private void SaveFragmentState(MainViewFragment fragment)
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

