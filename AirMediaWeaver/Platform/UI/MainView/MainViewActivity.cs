using System;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Base.Interface;
using AirMedia.Platform.UI.Library;
using AirMedia.Platform.UI.Player;
using AirMedia.Platform.UI.Playlists;
using AirMedia.Platform.UI.Publications;
using AirMedia.Platform.UI.Recommendations;
using AirMedia.Platform.UI.Search;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.Jeremyfeinstein.Slidingmenu.Lib;
using Fragment = Android.Support.V4.App.Fragment;

namespace AirMedia.Platform.UI.MainView
{
    [Activity(Label = "@string/ApplicationNameShorten", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainViewActivity : AmwActivity, IPlayerFacadeFragmentCallbacks, IMainViewFragmentCallbacks
    {
        public const string ExtraDisplayFragment = "display_fragment";

        private const string ExtraFragmentStateBundle = "fragment_state_bundle";
        private const string ContentFragmentTag = "content_fragment_tag";
        private const string PlayerFacadeFragmentTag = "player_facade_fragment_tag";

        private const int ContentDisplayTransactionDelayMillis = 330;

        private static readonly DrawerNavigationItem[] NavigationItems;

        private Bundle _fragmentStateBundle;
        private MainMenuListAdapter _mainMenuListAdapter;
        private ListView _menuListView;
        private SlidingMenu _slidingMenu;

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
                        },
                    new DrawerNavigationItem
                        {
                            StringResourceId = Resource.String.title_navigation_item_publications
                        },
                    new DrawerNavigationItem
                        {
                            StringResourceId = Resource.String.title_navigation_item_recommendations
                        },
                    new DrawerNavigationItem
                        {
                            StringResourceId = Resource.String.title_navigation_item_search
                        }
                };
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.Activity_MainView);
            
            _mainMenuListAdapter = new MainMenuListAdapter();
            _mainMenuListAdapter.SetItems(NavigationItems);

            #region Prepare Sliding Menu
            _slidingMenu = new SlidingMenu(this);
            _slidingMenu.SetMenu(Resource.Layout.View_Main_Menu);
            _slidingMenu.TouchModeAbove = SlidingMenu.TouchmodeFullscreen;
            _slidingMenu.SetTouchModeBehind(SlidingMenu.TouchmodeMargin);
            _slidingMenu.SetFadeEnabled(true);
            _slidingMenu.SetFadeDegree(0.35f);
            _slidingMenu.SetShadowDrawable(Resource.Drawable.main_menu_behind_shadow);
            _slidingMenu.SetBehindOffsetRes(Resource.Dimension.main_menu_offset);
            _slidingMenu.AttachToActivity(this, SlidingMenu.SlidingWindow);

            _menuListView = FindViewById<ListView>(Android.Resource.Id.List);
            _menuListView.Adapter = _mainMenuListAdapter;
            #endregion

            ActionBar.SetLogo(Resource.Drawable.ic_side_menu);
            ActionBar.SetDisplayUseLogoEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            Type displayFragmentType = null;
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
            if (displayFragmentType == null)
            {
                displayFragmentType = App.Preferences.LastMainView ?? typeof(LibraryTabsFragment);
            }

            SetContentFragment(displayFragmentType);

            var playerFacadeFragment = (PlayerFacadeFragment) SupportFragmentManager.FindFragmentByTag(PlayerFacadeFragmentTag);

            if (playerFacadeFragment == null)
            {
                playerFacadeFragment = new PlayerFacadeFragment();
                SupportFragmentManager.BeginTransaction()
                                      .Add(Resource.Id.playerView, playerFacadeFragment, PlayerFacadeFragmentTag)
                                      .Commit();
            }
        }

        public void RequestContentTitleUpdate(string title)
        {
            ActionBar.Title = title;
        }

        public void RequestNavigationTouchMode(MainMenuTouchMode mode)
        {
            if (_slidingMenu == null) return;

            _slidingMenu.TouchModeAbove = (int)mode;
        }

        protected override void OnResume()
        {
            base.OnResume();

            _menuListView.ItemClick += OnNavigationItemClicked;
            _slidingMenu.Open += OnSideMenuOpened;
            _slidingMenu.Close += OnSideMenuClosed;
        }

        protected override void OnPause()
        {
            _menuListView.ItemClick -= OnNavigationItemClicked;
            _slidingMenu.Open -= OnSideMenuOpened;
            _slidingMenu.Close -= OnSideMenuClosed;

            base.OnPause();
        }

        protected virtual void OnSideMenuOpened(object sender, EventArgs args)
        {
        }

        protected virtual void OnSideMenuClosed(object sender, EventArgs args)
        {
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

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            var fragment = GetContentFragment();
            if (fragment != null)
            {
                fragment.OnActivityResult(requestCode, (int)resultCode, data);

                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    if (_slidingMenu.IsMenuShowing == false)
                        _slidingMenu.Toggle(true);
                    return true;

                default:
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Menu)
            {
                _slidingMenu.Toggle(true);

                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        public override void OnBackPressed()
        {
            if (_slidingMenu.IsMenuShowing)
            {
                _slidingMenu.Toggle(true);
                return;
            }

            base.OnBackPressed();
        }

        private void OnNavigationItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            _slidingMenu.Toggle(true);

            Type contentFragment = null;
            var item = _mainMenuListAdapter[args.Position];
            switch (item.StringResourceId)
            {
                case Resource.String.title_navigation_item_media_library:
                    contentFragment = typeof (LibraryTabsFragment);
                    break;

                case Resource.String.title_navigation_item_playlists:
                    contentFragment = typeof(PlaylistsViewFragment);
                    break;

                case Resource.String.title_navigation_item_publications:
                    contentFragment = typeof(PublicationsTabFragment);
                    break;

                case Resource.String.title_navigation_item_recommendations:
                    contentFragment = typeof(RecommendationsFragment);
                    break;

                case Resource.String.title_navigation_item_search:
                    contentFragment = typeof(SearchFragment);
                    break;

                default:
                    AmwLog.Error(LogTag, "undefined navigation item clicked");
                    break;
            }

            if (contentFragment != null)
            {
                // Delayed start to let finish animations
                App.MainHandler.PostDelayed(() => SetContentFragment(contentFragment), 
                    ContentDisplayTransactionDelayMillis);
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

            if (currentFragment != null && currentFragment.GetType() == fragmentType)
                return;

            if (currentFragment != null) SaveFragmentState(currentFragment);

            string javaFragmentType = Java.Lang.Class.FromType(fragmentType).Name;
            currentFragment = (MainViewFragment)Fragment.Instantiate(this, javaFragmentType);

            var savedState = GetSavedState(fragmentType);
            if (savedState != null)
            {
                currentFragment.SetInitialSavedState(savedState);
            }

            SupportFragmentManager.BeginTransaction()
                                  .Replace(Resource.Id.contentView, currentFragment, ContentFragmentTag)
                                  .CommitAllowingStateLoss();

            App.Preferences.LastMainView = fragmentType;
        }

        private MainViewFragment GetContentFragment()
        {
            return SupportFragmentManager.FindFragmentByTag(ContentFragmentTag) as MainViewFragment;
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

            var details = (object)string.Format("saved state: \"{0}\"; type: \"{1}\"", 
                savedState, savedState.GetType().FullName);

            AmwLog.Error(LogTag, details, "Error retrieving fragment's state " +
                                          "for fragment \"{0}\"", fragmentType.Name);

            return null;
        }

        private void SaveFragmentState(MainViewFragment fragment)
        {
            if (_fragmentStateBundle == null) _fragmentStateBundle = new Bundle();

            try
            {
                var fragmentState = SupportFragmentManager.SaveFragmentInstanceState(fragment);
                _fragmentStateBundle.PutParcelable(fragment.GetType().Name, fragmentState);
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, e, "Can't save fragment state for \"{0}\". " +
                                        "Message: \"{1}\"", fragment.GetType().Name, e.Message);
            }
        }

        public void OnGenericPlaybackRequested()
        {
            var currentFragment = GetContentFragment();

            if (currentFragment != null)
            {
                currentFragment.OnGenericPlaybackRequested();
            }
        }
    }
}

