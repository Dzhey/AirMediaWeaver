using AirMedia.Core;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;
using Android.Widget;
using AirMedia.Platform.UI.Playlists;

namespace AirMedia.Platform.UI.Recommendations
{
    public class RecommendationsFragment : MainViewFragment
    {
        private ListView _listView;
        private PlaylistTracksAdapter _adapter;
        private IRequestFactory _recsRequestFactory;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new PlaylistTracksAdapter();
            var factory = RequestFactory.Init(typeof (LoadRecommendationsRequest));
            _recsRequestFactory = AndroidRequestFactory.Init(factory, ResultListener)
                                                       .SetActionTag(LoadRecommendationsRequest.ActionTagDefault)
                                                       .SetParallel(true)
                                                       .SetDistinct(true);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_Recommendations, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

            var progressPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, Consts.DefaultProgressDelayMillis,
                Resource.String.note_recommendations_list_empty);

            return view;
        }

        public override void OnDestroyView()
        {
            RemoveRequestUpdateHandler(typeof(LoadRecommendationsRequest));
            base.OnDestroyView();
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RegisterRequestUpdateHandler(typeof(LoadRecommendationsRequest), OnTrackListLoadUpdate);
            ReloadList();
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(LoadRecommendationsRequest), OnTrackListLoaded);

            _listView.ItemClick += OnListItemClicked;
        }

        public override void OnPause()
        {
            _listView.ItemClick += OnListItemClicked;

            RemoveRequestResultHandler(typeof(LoadRecommendationsRequest));

            base.OnPause();
        }

        private void ReloadList()
        {
            SetInProgress(true);

            _recsRequestFactory.Submit(App.MemoryRequestResultCache);
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_recommendations);
        }

        public override void OnGenericPlaybackRequested()
        {
            if (_adapter.Count < 1)
            {
                ShowMessage(Resource.String.error_cant_play_recommendations_list_empty);
                return;
            }

            PlayerControl.Play(_adapter.Items, 0);
        }

        public override bool HasDisplayedContent()
        {
            return _listView != null && _listView.Count > 0;
        }

        private void OnListItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            PlayerControl.Play(_adapter.Items, args.Position);
        }

        private void OnTrackListLoaded(object sender, ResultEventArgs args)
        {
            SetInProgress(false);

            if (args.Request.Status != RequestStatus.Ok)
            {
                ShowMessage(Resource.String.error_cant_load_recommendations);
                AmwLog.Error(LogTag, "Error loading recommdation track list");
            }
        }

        private void OnTrackListLoadUpdate(object sender, UpdateEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok 
                && args.Request.Status != RequestStatus.InProgress) return;

            switch (args.UpdateData.UpdateCode)
            {
                case UpdateData.UpdateCodeCachedResultRetrieved:
                    var cachedResult = ((CachedUpdateData)args.UpdateData).CachedResult;
                    var cachedData = ((LoadRecommendationsRequestResult)cachedResult).Data;
                    _adapter.SetItems(cachedData);
                    if (cachedData.Count > 0)
                        SetInProgress(false);
                    break;

                case UpdateData.UpdateCodeIntermediateResultObtained:
                    var result = ((IntermediateResultUpdateData)args.UpdateData).RequestResult;
                    var data = ((LoadRecommendationsRequestResult)result).Data;
                    _adapter.SetItems(data);
                    if (data.Count > 0)
                        SetInProgress(false);
                    break;
            }
        }
    }
}