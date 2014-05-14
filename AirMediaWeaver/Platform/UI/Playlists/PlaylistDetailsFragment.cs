using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Data;
using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistDetailsFragment : AmwFragment
    {
        public const string ExtraPlaylistId = "playlist_id";

        private PlaylistItemsAdapter _adapter;
        private ListView _listView;
        private View _progressPanel;
        private View _emptyIndicatorView;
        private long? _playlistId;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Activity.ActionBar.SetTitle(Resource.String.title_playlist_details);

            _adapter = new PlaylistItemsAdapter();

            Activity.ActionBar.SetDisplayShowHomeEnabled(true);
            Activity.ActionBar.SetDisplayHomeAsUpEnabled(true);

            if (Arguments != null && Arguments.ContainsKey(ExtraPlaylistId))
            {
                _playlistId = Arguments.GetLong(ExtraPlaylistId);

                var playlist = PlaylistManager.GetPlaylist((long)_playlistId);
                if (playlist != null)
                {
                    Activity.ActionBar.Title = playlist.Name;
                }
            }
            else
            {
                AmwLog.Error(LogTag, "playlist id is not specified to display content");
            }
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_PlaylistDetails, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

            _progressPanel = view.FindViewById(Android.Resource.Id.Progress);
            _emptyIndicatorView = view.FindViewById(Android.Resource.Id.Empty);

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            if (_playlistId != null)
            {
                UpdateProgressIndicators(true);
                SubmitParallelRequest(new LoadPlaylistItemsRequest((long) _playlistId));
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Activity.Finish();

                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(LoadPlaylistItemsRequest), OnPlaylistItemsLoaded);
        }

        public override void OnPause()
        {
            RemoveRequestResultHandler(typeof(LoadPlaylistItemsRequest));

            base.OnPause();
        }

        private void OnPlaylistItemsLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                UpdateProgressIndicators(false);
                AmwLog.Error(LogTag, "Error loading playlist tracks");
                return;
            }

            var metadata = ((LoadRequestResult<List<TrackMetadata>>) args.Result).Data;
            if (_adapter != null)
            {
                _adapter.SetItems(metadata);
            }
            UpdateProgressIndicators(false);
        }

        private void UpdateProgressIndicators(bool isInProgress)
        {
            if (_listView == null || _listView.Count == 0)
            {
                if (isInProgress)
                {
                    _progressPanel.Visibility = ViewStates.Visible;
                    _emptyIndicatorView.Visibility = ViewStates.Gone;
                }
                else
                {
                    _progressPanel.Visibility = ViewStates.Gone;
                    _emptyIndicatorView.Visibility = ViewStates.Visible;
                }
            }
            else
            {
                _progressPanel.Visibility = ViewStates.Gone;
                _emptyIndicatorView.Visibility = ViewStates.Gone;
            }
        }
    }
}