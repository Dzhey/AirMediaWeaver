using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.UI.Base;
using AirMedia.Platform.UI.Library;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistDetailsFragment : MainViewFragment
    {
        public const string ExtraPlaylistId = "playlist_id";

        private const int RequestCodeEditPlaylist = 1000;

        private PlaylistTracksAdapter _adapter;
        private ListView _listView;
        private long? _playlistId;
        private string _playlistName;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Activity.ActionBar.SetTitle(Resource.String.title_playlist_details);

            _adapter = new PlaylistTracksAdapter();

            SetHasOptionsMenu(true);
            Activity.ActionBar.SetDisplayShowHomeEnabled(true);
            Activity.ActionBar.SetDisplayHomeAsUpEnabled(true);

            if (Arguments != null && Arguments.ContainsKey(ExtraPlaylistId))
            {
                _playlistId = Arguments.GetLong(ExtraPlaylistId);

                var playlist = PlaylistDao.GetPlaylist((long)_playlistId);
                if (playlist != null)
                {
                    _playlistName = playlist.Name;
                    Activity.ActionBar.Title = _playlistName;
                }
            }
            else
            {
                AmwLog.Error(LogTag, "playlist id is not specified to display content");
            }
        }

        public override string GetTitle()
        {
            return _playlistName;
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.menu_playlists_details, menu);
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_PlaylistDetails, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

            var progressPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progressPanel, 0, Resource.String.note_playlist_empty);

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            ReloadList();
        }

        public override void OnGenericPlaybackRequested()
        {
            if (_playlistId != null)
            {
                SubmitRequest(new PlayPlaylistRequest((long)_playlistId));
            }
        }

        public override bool HasDisplayedContent()
        {
            return _listView != null && _listView.Count > 0;
        }

        private void ReloadList()
        {
            if (_playlistId != null)
            {
                SetInProgress(true);
                SubmitParallelRequest(new LoadPlaylistItemsRequest((long) _playlistId));
            }
            else
            {
                AmwLog.Error(LogTag, "Can't reload playlist - playlistId is not specified");
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Activity.Finish();
                    return true;

                case Resource.Id.ActionEdit:
                    var args = new Bundle();
                    args.PutBoolean(AudioLibraryFragment.ExtraStartInPickMode, true);
                    args.PutLongArray(AudioLibraryFragment.ExtraCheckedTrackIds, _adapter.GetItemIds());
                    var intent = FragmentContentActivity.CreateStartIntent(
                        Activity, typeof (AudioLibraryFragment), args);
                    Activity.StartActivityForResult(intent, RequestCodeEditPlaylist);
                    return true;

                case Resource.Id.ActionPlay:
                    if (_adapter.Count < 1 || _playlistId == null)
                    {
                        ShowMessage(Resource.String.error_cant_play_playlist_no_tracks);
                    }
                    else
                    {
                        SubmitRequest(new PlayPlaylistRequest((long)_playlistId));
                    }

                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            switch (requestCode)
            {
                case RequestCodeEditPlaylist:
                    if (resultCode == (int)Result.Ok)
                    {
                        var selectedTracks = data.GetLongArrayExtra(AudioLibraryFragment.ExtraCheckedTrackIds);
                        if (_playlistId == null || PlaylistDao.UpdatePlaylistContents(
                            (long)_playlistId, selectedTracks) == false)
                        {
                            ShowMessage(Resource.String.error_cant_update_playlists);
                        }
                        
                        ReloadList();
                    }

                    return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(LoadPlaylistItemsRequest), OnPlaylistItemsLoaded);
            RegisterRequestResultHandler(typeof(PlayPlaylistRequest), OnPlayPlaylistRequestFinished);

            _listView.ItemClick += OnTrackItemClicked;
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnTrackItemClicked;

            RemoveRequestResultHandler(typeof(LoadPlaylistItemsRequest));
            RemoveRequestResultHandler(typeof(PlayPlaylistRequest));

            base.OnPause();
        }

        private void OnTrackItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            if (_playlistId == null)
            {
                ShowMessage(Resource.String.error_cant_play_playlist_no_tracks);
                return;
            }

            SubmitRequest(new PlayPlaylistRequest((int) _playlistId, args.Position));
        }

        private void OnPlaylistItemsLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                SetInProgress(false);
                AmwLog.Error(LogTag, "Error loading playlist tracks");
                return;
            }

            var metadata = ((LoadRequestResult<List<ITrackMetadata>>) args.Result).Data;
            if (_adapter != null)
            {
                _adapter.SetItems(metadata);
            }
            SetInProgress(false);
        }

        private void OnPlayPlaylistRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                switch (args.Result.ResultCode)
                {
                    case PlayPlaylistRequest.ResultCodeErrorNoTracksAvailable:
                        ShowMessage(Resource.String.error_cant_play_playlist_no_tracks);
                        break;

                    default:
                        AmwLog.Error(LogTag, "error trying to enqueue playlist to playback");
                        ShowMessage(Resource.String.error_cant_play_playlist);
                        break;
                }
            }
        }
    }
}