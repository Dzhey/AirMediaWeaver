using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.UI.Base;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistsViewFragment : MainViewFragment
    {
        private ListView _listView;
        private View _progressPanel;
        private View _emptyIndicatorView;
        private PlaylistListAdapter _adapter;

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_PlaylistsView, container, false);

            _adapter = new PlaylistListAdapter();

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            _listView.Adapter = _adapter;

            _progressPanel = view.FindViewById(Android.Resource.Id.Progress);
            _emptyIndicatorView = view.FindViewById(Android.Resource.Id.Empty);

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            UpdateProgressIndicators(true);
            SubmitParallelRequest(new LoadPlaylistsRequest());
        }

        public override void OnResume()
        {
            base.OnResume();

            _listView.ItemClick += OnPlaylistClicked;
            RegisterRequestResultHandler(typeof(LoadPlaylistsRequest), OnPlaylistsLoaded);
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_playlists_view);
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnPlaylistClicked;
            RemoveRequestResultHandler(typeof(LoadPlaylistsRequest));

            base.OnPause();
        }

        private void OnPlaylistClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            long playlistId = _adapter[args.Position].Id;

            var fragmentArgs = new Bundle();
            fragmentArgs.PutLong(PlaylistDetailsFragment.ExtraPlaylistId, playlistId);

            FragmentContentActivity.StartAcitvity(Activity, 
                typeof(PlaylistDetailsFragment), fragmentArgs);
        }

        private void OnPlaylistsLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                UpdateProgressIndicators(false);
                AmwLog.Error(LogTag, "error loading playlists");
                return;
            }

            var playlists = ((LoadRequestResult<List<PlaylistModel>>) args.Result).Data;
            if (_adapter != null)
            {
                _adapter.SetItems(playlists);
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