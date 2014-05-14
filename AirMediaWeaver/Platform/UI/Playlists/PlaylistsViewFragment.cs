using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Playlists
{
    public class PlaylistsViewFragment : MainViewFragment
    {
        private const string TagCreatePlaylistDialog = "dialog_create_playlist";
        private const int RequestCodeEditPlaylist = 10;

        private ListView _listView;
        private View _progressPanel;
        private View _emptyIndicatorView;
        private PlaylistListAdapter _adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetHasOptionsMenu(true);
        }

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

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.menu_playlists_view, menu);

            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ActionNew:
                    DisplayCreatePlaylistDialog();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnActivityResult(int requestCode, 
            Android.App.Result resultCode, Intent data)
        {
            if (requestCode == RequestCodeEditPlaylist)
            {
                ReloadList();

                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            ReloadList();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_playlists_view);
        }

        public override void OnResume()
        {
            base.OnResume();

            _listView.ItemClick += OnPlaylistClicked;
            RegisterRequestResultHandler(typeof(LoadPlaylistsRequest), OnPlaylistsLoaded);

            var createPlaylistDialog = (InputTextDialogFragment) FragmentManager
                .FindFragmentByTag(TagCreatePlaylistDialog);

            if (createPlaylistDialog != null)
            {
                createPlaylistDialog.AcceptClick += OnNewPlaylistNameEntered;
            }
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnPlaylistClicked;
            RemoveRequestResultHandler(typeof(LoadPlaylistsRequest));

            var createPlaylistDialog = (InputTextDialogFragment)FragmentManager
                .FindFragmentByTag(TagCreatePlaylistDialog);

            if (createPlaylistDialog != null)
            {
                createPlaylistDialog.AcceptClick -= OnNewPlaylistNameEntered;
            }

            base.OnPause();
        }

        private void DisplayCreatePlaylistDialog()
        {
            string title = GetString(Resource.String.dialog_create_playlist_title);
            string confirmText = GetString(Resource.String.dialog_create_playlist_confirm);
            var dialog = InputTextDialogFragment.NewInstance(Activity, title, acceptText: confirmText);

            dialog.AcceptClick += OnNewPlaylistNameEntered;

            dialog.Show(FragmentManager, TagCreatePlaylistDialog);
        }

        private void OnNewPlaylistNameEntered(object sender, Bundle payload)
        {
            var dialog = (InputTextDialogFragment) sender;
            string text = dialog.InputText;

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowMessage(Resource.String.error_empty_playlist_name);
                return;
            }

            string textTrimmed = text.Trim();
            var playlistModel = PlaylistManager.CreateNewPlaylist(textTrimmed);
            if (playlistModel == null)
            {
                ShowMessage(Resource.String.error_cant_create_playlist);
                AmwLog.Error(LogTag, string.Format("cant create playlist for name \"{0}\"", text.Trim()));
            }
            else
            {
                OpenPlaylistDetails(playlistModel.Id);
            }
        }

        private void OnPlaylistClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            long playlistId = _adapter[args.Position].Id;

            OpenPlaylistDetails(playlistId);
        }

        private void OpenPlaylistDetails(long playlistId)
        {
            var fragmentArgs = new Bundle();
            fragmentArgs.PutLong(PlaylistDetailsFragment.ExtraPlaylistId, playlistId);

            var intent = FragmentContentActivity.CreateStartIntent(Activity,
                typeof(PlaylistDetailsFragment), fragmentArgs);

            // Delayed start to let finish animations
            App.MainHandler.PostDelayed(() => 
                Activity.StartActivityForResult(intent, RequestCodeEditPlaylist), 200);
        }

        private void ReloadList()
        {
            UpdateProgressIndicators(true);
            SubmitParallelRequest(new LoadPlaylistsRequest());
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