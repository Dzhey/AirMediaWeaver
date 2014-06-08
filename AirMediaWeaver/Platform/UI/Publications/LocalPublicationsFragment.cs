using System;
using AirMedia.Core;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.DownloadManager;
using AirMedia.Platform.Controller.Receivers;
using AirMedia.Platform.Data.Sql.Dao;
using AirMedia.Platform.Data.Sql.Model;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Publications
{
    public class LocalPublicationsFragment : MainViewFragment
    {
        private ListView _listView;
        private DownloadTrackListAdapter _adapter;
        private AmwDownloadManager _downloadManager;
        private RemoteTrackPublicationsUpdateReceiver _tracksUpdateReceiver;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new DownloadTrackListAdapter();
            _adapter.DisplayDownloadButton = true;

            _tracksUpdateReceiver = new RemoteTrackPublicationsUpdateReceiver();

            var trackDownloadsDao = (TrackDownloadsDao) DatabaseHelper.Instance.GetDao<TrackDownloadRecord>();
            _downloadManager = new AmwDownloadManager(Activity, trackDownloadsDao);
        }

        public override void OnDestroy()
        {
            _downloadManager = null;

            base.OnDestroy();
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

        public override void OnStart()
        {
            base.OnStart();

            _tracksUpdateReceiver.RemoteTrackPublicationsUpdate += OnTrackPublicationsUpdated;

            var filter = new IntentFilter(RemoteTrackPublicationsUpdateReceiver.ActionRemoteTrackPublicationsUpdated);
            Activity.RegisterReceiver(_tracksUpdateReceiver, filter);
        }

        public override void OnStop()
        {
            _tracksUpdateReceiver.RemoteTrackPublicationsUpdate -= OnTrackPublicationsUpdated;
            Activity.UnregisterReceiver(_tracksUpdateReceiver);

            base.OnStop();
        }

        public override void OnResume()
        {
            base.OnResume();

            _listView.ItemClick += OnListItemClicked;
            _adapter.DownloadClicked += OnDownloadItemClicked;

            RegisterRequestResultHandler(typeof(LoadRemoteTrackDownloadsRequest), OnRemoteTracksLoaded);
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnListItemClicked;
            _adapter.DownloadClicked -= OnDownloadItemClicked;

            RemoveRequestResultHandler(typeof(LoadRemoteTrackDownloadsRequest));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_lan_publications);
        }

        public override void OnGenericPlaybackRequested()
        {
            var itemGuids = _adapter.GetItemGuids();

            if (itemGuids.Length < 1)
            {
                ShowMessage(Resource.String.error_cant_start_playback_no_publications_found);
                return;
            }

            PlayerControl.Play(itemGuids);
        }

        public override bool HasDisplayedContent()
        {
            return _listView.Count > 0;
        }

        private void OnDownloadItemClicked(object sender, DownloadTrackListAdapter.ItemDownloadClickEventArgs args)
        {
            var metadata = args.TrackMetadata;

            try
            {
                _downloadManager.EnqueueDownload(metadata.TrackGuid);
                _adapter.AddDownloadTrackGuid(metadata.TrackGuid);
            }
            catch (ArgumentException e)
            {
                AmwLog.Error(LogTag, e, "cant start track download: {0}", e.Message);
                ShowMessage(Resource.String.error_cant_begin_track_download);
            }
        }

        private void OnListItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            PlayerControl.Play(_adapter.GetItemGuids(), args.Position);
        }

        private void OnTrackPublicationsUpdated(object sender, EventArgs args)
        {
            ReloadList();
        }

        private void ReloadList()
        {
            SetInProgress(true);

            SubmitParallelRequest(new LoadRemoteTrackDownloadsRequest());
        }

        private void OnRemoteTracksLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "failed to load base tracks info");
                ShowMessage(Resource.String.error_cant_load_lan_publications);
                SetInProgress(false);
                return;
            }

            var result = (LoadRemoteTrackDownloadsRequest.RequestResult) args.Result;
            _adapter.SetItems(result.Data);
            _adapter.SetDownloadTrackGuids(result.DownloadTrackGuids);

            SetInProgress(false);
        }
    }
}