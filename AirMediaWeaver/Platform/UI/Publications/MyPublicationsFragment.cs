using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Data;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library;
using AirMedia.Platform.UI.Playlists;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.UI.Publications
{
    public class MyPublicationsFragment : MainViewFragment
    {
        private const int RequestCodeEditPublications = 3000;

        private ListView _listView;
        private TrackListAdapter _adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _adapter = new TrackListAdapter();
            SetHasOptionsMenu(true);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.menu_publications_view, menu);
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_MyPublications, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

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
            RegisterRequestResultHandler(typeof(UpdatePublishedTracksRequest), OnUpdatePublishedTracksResult);
        }

        public override void OnPause()
        {
            RemoveRequestResultHandler(typeof(LoadPublishedTracksRequest));
            RemoveRequestResultHandler(typeof(UpdatePublishedTracksRequest));

            base.OnPause();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ActionEdit:
                    var args = new Bundle();
                    args.PutBoolean(AudioLibraryFragment.ExtraStartInPickMode, true);
                    args.PutLongArray(AudioLibraryFragment.ExtraCheckedTrackIds, _adapter.GetItemIds());
                    var intent = FragmentContentActivity.CreateStartIntent(
                        Activity, typeof (AudioLibraryFragment), args);
                    Activity.StartActivityForResult(intent, RequestCodeEditPublications);
                    break;

                default:
                    return base.OnOptionsItemSelected(item);
            }

            return true;
        }

        public override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (requestCode)
            {
                case RequestCodeEditPublications:
                    var selectedTracks = data.GetLongArrayExtra(AudioLibraryFragment.ExtraCheckedTrackIds);
                    SubmitRequest(new UpdatePublishedTracksRequest(selectedTracks));
                    break;

                default:
                    base.OnActivityResult(requestCode, resultCode, data);
                    break;
            }
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

        private void OnUpdatePublishedTracksResult(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                ShowMessage(Resource.String.error_cant_update_publications);
                AmwLog.Error(LogTag, "Error trying to update track publications");
            }

            SubmitParallelRequest(new LoadPublishedTracksRequest());
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
            _adapter.SetItems(data);

            SetInProgress(false);
        }
    }
}