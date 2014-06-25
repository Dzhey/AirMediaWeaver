using AirMedia.Core;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumListFragment : MainViewFragment, AlbumListGridAdapter.ICallbacks
    {
        /// <summary>
        /// 3 items per second thresold to temporarilly disable album arts loader
        /// </summary>
        private const int AlbumArtsLoaderDisableThreshold = 3;

        private ListView _albumListView;
        private AlbumListGridAdapter _listAdapter;
        private IRequestFactory _loadRequestFactory;
        private bool _isContentReloaded;
        private View _contentPlaceholder;
        private int _lastScrollAlbumItemPosition;
        private long _previousScrollEventTime;
        private double _albumListScrollSpeed;

        public override bool UserVisibleHint
        {
            get
            {
                return base.UserVisibleHint;
            }

            set
            {
                base.UserVisibleHint = value;

                if (UserVisibleHint && !_isContentReloaded)
                    ReloadList();
            }
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var factory = RequestFactory.Init(typeof (AndroidLoadLocalArtistAlbumsRequest));
            _loadRequestFactory = AndroidRequestFactory.Init(factory, ResultListener)
                                                       .SetParallel(true)
                                                       .SetDistinct(true)
                                                       .SetActionTag(AndroidLoadLocalArtistAlbumsRequest.ActionTagDefault);
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
           var view = inflater.Inflate(Resource.Layout.Fragment_AlbumList, container, false);
           _contentPlaceholder = view.FindViewById(Resource.Id.contentPlaceholder);

           _albumListView = view.FindViewById<ListView>(Android.Resource.Id.List);
            var progresPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progresPanel, Consts.DefaultProgressDelayMillis, 
                Resource.String.note_audio_library_empty);

            _listAdapter = new AlbumListGridAdapter(this);
            _albumListView.Adapter = _listAdapter;

            _isContentReloaded = false;

            return view;
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
                _listAdapter.IsAlbumArtsLoaderEnabled = false;
            else
                _listAdapter.IsAlbumArtsLoaderEnabled = true;
        }

        private void OnAlbumListScrollStateChanged(object sender, AbsListView.ScrollStateChangedEventArgs args)
        {
            if (args.ScrollState == ScrollState.Idle)
                _listAdapter.IsAlbumArtsLoaderEnabled = true;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RegisterRequestUpdateHandler(typeof(AndroidLoadLocalArtistAlbumsRequest), OnLoadRequestUpdate);
        }

        public override void OnDestroyView()
        {
            RemoveRequestUpdateHandler(typeof(AndroidLoadLocalArtistAlbumsRequest));

            if (_albumListView != null && _albumListView.Adapter != null)
            {
                _albumListView.Adapter.Dispose();
                _albumListView.Adapter = null;
            }

            base.OnDestroyView();
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(AndroidLoadLocalArtistAlbumsRequest), OnLoadRequestFinished);
            _albumListView.Scroll += OnAlbumListScrollEvent;
            _albumListView.ScrollStateChanged += OnAlbumListScrollStateChanged;
        }

        public override void OnPause()
        {
            _albumListView.Scroll -= OnAlbumListScrollEvent;
            _albumListView.ScrollStateChanged -= OnAlbumListScrollStateChanged;
            RemoveRequestResultHandler(typeof(AndroidLoadLocalArtistAlbumsRequest));

            base.OnPause();
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
            bool hasContent = _listAdapter.Count > 0;

            if (hasContent && _contentPlaceholder.Visibility != ViewStates.Gone)
                _contentPlaceholder.Visibility = ViewStates.Gone;

            return hasContent;
        }

        protected void ReloadList()
        {
            SetInProgress(true);
            _loadRequestFactory.Submit();
            _isContentReloaded = true;
        }

        private void OnLoadRequestFinished(object sender, ResultEventArgs args)
        {
            SetInProgress(false);
            if (args.Request.Status != RequestStatus.Ok)
            {
                ShowMessage(Resource.String.error_cant_load_data);
                AmwLog.Error(LogTag, "Error loading local album list");
            }

            RequestManager.Instance.TryDisposeRequest(args.Request.RequestId);
        }

        private void OnLoadRequestUpdate(object sender, UpdateEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok
                  && args.Request.Status != RequestStatus.InProgress) return;

            switch (args.UpdateData.UpdateCode)
            {
                case UpdateData.UpdateCodeCachedResultRetrieved:
                    var cachedResult = ((CachedUpdateData)args.UpdateData).CachedResult;
                    var cachedData = ((LoadArtistAlbumsRequestResult)cachedResult).Data;
                    _listAdapter.SetItems(cachedData);
                    
                    if (cachedData.Count > 0)
                        SetInProgress(false);
                    break;

                case UpdateData.UpdateCodeIntermediateResultObtained:
                    var wrappedResult = (IntermediateResultUpdateData) args.UpdateData;
                    var result = (LoadArtistAlbumsRequestResult) wrappedResult.RequestResult;
                    _listAdapter.SetItems(result.Data);
                    _listAdapter.AddAlbumArts(result.AlbumArts);
                    if (result.Data.Count > 0)
                        SetInProgress(false);
                    break;
            }
        }

        public void OnLowMemoryDetected()
        {
            ShowMessage(Resource.String.warning_out_of_memory);
        }

        public AbsListView GetListView()
        {
            return _albumListView;
        }
    }
}