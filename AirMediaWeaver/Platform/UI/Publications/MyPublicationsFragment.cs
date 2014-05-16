using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Data;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Playlists;
using Android.OS;
using Android.Views;
using Android.Widget;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.UI.Publications
{
    public class MyPublicationsFragment : MainViewFragment
    {
        private ListView _listView;
        private TrackListAdapter _trackListAdapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _trackListAdapter = new TrackListAdapter();
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_AudioLibrary, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _trackListAdapter;

            var progressPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, Consts.DefaultProgressDelayMillis, 
                Resource.String.note_my_publication_list_empty);

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            SetInProgress(true);
            SubmitParallelRequest(new LoadPublishedTracksRequest());
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(LoadPublishedTracksRequest), OnTrackListLoaded);
        }

        public override void OnPause()
        {
            RemoveRequestResultHandler(typeof(LoadPublishedTracksRequest));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_my_publications);
        }

        public override void OnGenericPlaybackRequested()
        {
            AmwLog.Info(LogTag, "TODO: enqueue publicated tracks");
        }

        public override bool HasDisplayedContent()
        {
            return _listView != null && _listView.Count > 0;
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

            var data = ((LoadRequestResult<List<TrackMetadata>>) args.Result).Data;
            _trackListAdapter.SetItems(data);

            SetInProgress(false);
        }
    }
}