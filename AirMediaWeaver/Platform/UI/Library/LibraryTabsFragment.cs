using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Support.V4.View;
using Android.Views;

namespace AirMedia.Platform.UI.Library
{
    public class LibraryTabsFragment : MainViewFragment
    {
        private TabItem[] _tabs;
        private LibraryTabsAdapter _pagerAdapter;
        private ViewPager _viewPager;
        private PagerSlidingTabStrip.PagerSlidingTabStrip _pageIndicator;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _pagerAdapter = new LibraryTabsAdapter(ChildFragmentManager);
            _tabs = CreateTabs();
            _pagerAdapter.SetItems(_tabs);
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_LibraryTabs, container, false);

            _viewPager = view.FindViewById<ViewPager>(Resource.Id.pager);
            _viewPager.Adapter = _pagerAdapter;
            _pageIndicator = view.FindViewById<PagerSlidingTabStrip.PagerSlidingTabStrip>(Resource.Id.pageIndicator);
            _pageIndicator.SetViewPager(_viewPager);

            return view;
        }

        protected virtual TabItem[] CreateTabs()
        {
            return new[]
                {
                    new TabItem
                        {
                            TabFragmentType = typeof(TrackListFragment),
                            Title = GetString(Resource.String.title_tab_artists)
                        },
                    new TabItem
                        {
                            TabFragmentType = typeof(TrackListFragment),
                            Title = GetString(Resource.String.title_tab_albums)
                        },
                    new TabItem
                        {
                            TabFragmentType = typeof(TrackListFragment),
                            Title = GetString(Resource.String.title_tab_tracks)
                        },
                    new TabItem
                        {
                            TabFragmentType = typeof(TrackListFragment),
                            Title = GetString(Resource.String.title_tab_folders)
                        },
                };
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_audio_library);
        }

        public override void OnGenericPlaybackRequested()
        {
        }

        public override bool HasDisplayedContent()
        {
            return true;
        }
    }
}