using AirMedia.Core;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library.AlbumList.Adapter;
using AirMedia.Platform.UI.Library.AlbumList.Controller;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using AirMedia.Platform.UI.ViewUtils.QuickActionHelper;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumListFragment : MainViewFragment, IAlbumListAdapterCallbacks, IAlbumListContentWorkerCallbacks
    {
        private static readonly ContentWorkerCreator AlbumsContentWorkerCreator = new ContentWorkerCreator();

        /// <summary>
        /// 3 items per second thresold to temporarily disable album arts loader
        /// </summary>
        private const int AlbumArtsLoaderDisableThreshold = 3;

        private AbsListView _albumListView;
        private IAlbumListContentWorker _contentController;
        private bool _isContentReloaded;
        private View _contentPlaceholder;
        private ViewGroup _contentContainer;
        private int _lastScrollAlbumItemPosition;
        private long _previousScrollEventTime;
        private double _albumListScrollSpeed;
        private IAlbumListAdapter _adapter;

        public override bool UserVisibleHint
        {
            get
            {
                return base.UserVisibleHint;
            }
            set
            {
                base.UserVisibleHint = value;

                if (value && !_isContentReloaded)
                    ReloadContent();
            }
        }

        public void ShowMessage(int stringResourceId)
        {
            ShowMessage(stringResourceId, ToastLength.Short);
        }

        public void OnContentDataLoaded(bool hasContentData)
        {
            if (hasContentData == false) return;

            SetInProgress(false);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetHasOptionsMenu(true);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.menu_album_list, menu);
        }

        private void InitContentView(LayoutInflater inflater, View fragmentView)
        {
            if (_contentController != null)
            {
                RemoveRequestWorker(_contentController);
                _contentController = null;
            }

            _contentController = AlbumsContentWorkerCreator.CreateContentWorker(
                ContentWorkerCreator.AlbumListAppearanceGrid,
                App.Preferences.AlbumListGrouping,
                this);

            _contentContainer.RemoveAllViews();
            if (_adapter != null)
            {
                _adapter.ItemClicked -= OnAlbumItemClicked;
                _adapter.ItemMenuClicked -= OnAlbumItemMenuClicked;
                _adapter.Callbacks = null;
            }
            if (_albumListView != null)
            {
                _albumListView.Adapter = null;
            }

            _albumListView = _contentController.InflateContainerView(inflater, _contentContainer, true);
            var progressPanel = fragmentView.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, Consts.DefaultProgressDelayMillis,
                Resource.String.note_audio_library_empty);

            _adapter = _contentController.Adapter;
            _adapter.Callbacks = this;
            _albumListView.Adapter = _adapter;

            if (IsResumed)
            {
                _adapter.ItemClicked += OnAlbumItemClicked;
                _adapter.ItemMenuClicked += OnAlbumItemMenuClicked;
            }

            RegisterRequestWorker(_contentController);
            _isContentReloaded = false;
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_AlbumList, container, false);
            _contentPlaceholder = view.FindViewById(Resource.Id.contentPlaceholder);
            _contentContainer = view.FindViewById<ViewGroup>(Resource.Id.contentViewContainer);

            InitContentView(inflater, view);

            return view;
        }

        public override void OnDestroyView()
        {
            _isContentReloaded = false;
            RemoveRequestWorker(_contentController);

            base.OnDestroyView();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ActionTogglePageLayout:
                    ToggleAlbumListGrouping();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ToggleAlbumListGrouping()
        {
            if (_adapter == null) return;

            switch (App.Preferences.AlbumListGrouping)
            {
                case ContentWorkerCreator.AlbumListGroupingByArtist:
                    App.Preferences.AlbumListGrouping = ContentWorkerCreator.AlbumListGroupingNone;
                    break;

                case ContentWorkerCreator.AlbumListGroupingNone:
                    App.Preferences.AlbumListGrouping = ContentWorkerCreator.AlbumListGroupingByArtist;
                    break;
            }

            InitContentView(LayoutInflater.From(Activity), View);
            ReloadContent();
        }

        private void OnAlbumListScrollEvent(object sender, AbsListView.ScrollEventArgs args)
        {
            if (_lastScrollAlbumItemPosition == args.FirstVisibleItem)
                return;

            var currentTime = JavaSystem.CurrentTimeMillis();
            var deltaTime = currentTime - _previousScrollEventTime;
            _albumListScrollSpeed = ((double) 1/deltaTime)*1000;

            _lastScrollAlbumItemPosition = args.FirstVisibleItem;
            _previousScrollEventTime = currentTime;

            if (_albumListScrollSpeed >= AlbumArtsLoaderDisableThreshold)
            {
                _contentController.IsAlbumArtsLoaderEnabled = false;
            }
            else
            {
                _contentController.IsAlbumArtsLoaderEnabled = true;
            }
        }

        private void OnAlbumListScrollStateChanged(object sender, AbsListView.ScrollStateChangedEventArgs args)
        {
            if (args.ScrollState == ScrollState.Idle)
            {
                _contentController.IsAlbumArtsLoaderEnabled = true;
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            _albumListView.Scroll += OnAlbumListScrollEvent;
            _albumListView.ScrollStateChanged += OnAlbumListScrollStateChanged;

            _adapter.ItemClicked += OnAlbumItemClicked;
            _adapter.ItemMenuClicked += OnAlbumItemMenuClicked;
        }

        public override void OnPause()
        {
            _adapter.ItemMenuClicked -= OnAlbumItemMenuClicked;
            _adapter.ItemClicked -= OnAlbumItemClicked;

            _albumListView.Scroll -= OnAlbumListScrollEvent;
            _albumListView.ScrollStateChanged -= OnAlbumListScrollStateChanged;

            base.OnPause();
        }

        private void OnAlbumItemClicked(object sender, AlbumItemClickEventArgs args)
        {
            ShowMessage("item clicked: " + args.Item.AlbumName);
        }

        private void OnAlbumItemMenuClicked(object sender, AlbumItemClickEventArgs args)
        {
            var popupHelper = new PopupActionHelper();
            popupHelper.ShowAlbumItemMenu(args.ClickedView);
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_tab_albums);
        }

        public override void OnGenericPlaybackRequested()
        {
        }

        public override bool HasDisplayedContent()
        {
            bool hasContent = _adapter.Count > 0;

            if (hasContent && _contentPlaceholder.Visibility != ViewStates.Gone)
                _contentPlaceholder.Visibility = ViewStates.Gone;

            return hasContent;
        }

        protected void ReloadContent()
        {
            SetInProgress(true);
            _contentController.PerformRequest();
            _isContentReloaded = true;
        }

        public override void OnLowMemory()
        {
            ShowMessage(Resource.String.warning_out_of_memory);
        }

        public AbsListView GetListView()
        {
            return _albumListView;
        }

        public void OnWorkerRequestError(int errorCode, string errorMessage)
        {
        }

        public void OnWorkerRequestFinished(ResultEventArgs args)
        {
            SetInProgress(false);
        }

        public void OnWorkerRequestUpdate(UpdateEventArgs args)
        {
        }
    }
}