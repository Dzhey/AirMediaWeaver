using AirMedia.Core;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Playlists;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Publications
{
    public class LocalPublicationsFragment : MainViewFragment
    {
        private ListView _listView;
        private TrackListAdapter _adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new TrackListAdapter();
        }

        public override View OnCreateView(LayoutInflater inflater,
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_MyPublications, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

            var progressPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, Consts.DefaultProgressDelayMillis,
                Resource.String.note_lan_publication_list_empty);

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

            RegisterRequestResultHandler(typeof(DownloadBaseTracksInfoRequestImpl), OnLocalPublicationsLoaded);
        }

        public override void OnPause()
        {
            RemoveRequestResultHandler(typeof(DownloadBaseTracksInfoRequestImpl));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_lan_publications);
        }

        public override void OnGenericPlaybackRequested()
        {
            ShowMessage("TODO: generic playback");
        }

        public override bool HasDisplayedContent()
        {
            return _listView.Count > 0;
        }

        private void ReloadList()
        {
            SetInProgress(true);

            SubmitParallelRequest(new DownloadBaseTracksInfoRequestImpl());
        }

        private void OnLocalPublicationsLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "failed to load base tracks info");
                ShowMessage(Resource.String.error_cant_load_lan_publications);
                SetInProgress(false);
                return;
            }

            var result = ((DownloadBaseTracksInfoRequest.RequestResult) args.Result).TrackInfo;
            _adapter.SetItems(result);

            SetInProgress(false);
        }
    }
}