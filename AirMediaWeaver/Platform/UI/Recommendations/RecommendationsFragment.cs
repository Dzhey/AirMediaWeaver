using System.Collections.Generic;
using AirMedia.Core;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
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

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new PlaylistTracksAdapter();
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

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

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
            SubmitParallelRequest(new LoadRecommendationsRequest());
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_recommendations);
        }

        public override void OnGenericPlaybackRequested()
        {
            ShowMessage("todo");
        }

        public override bool HasDisplayedContent()
        {
            return _listView != null && _listView.Count > 0;
        }

        private void OnListItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            ShowMessage("todo: play");
        }

        private void OnTrackListLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                SetInProgress(false);
                ShowMessage(Resource.String.error_cant_load_published_tracklist);
                AmwLog.Error(LogTag, "Error loading published track list");
                return;
            }

            var data = ((LoadRequestResult<List<IRemoteTrackMetadata>>) args.Result).Data;
            _adapter.SetItems(data);

            SetInProgress(false);
        }
    }
}