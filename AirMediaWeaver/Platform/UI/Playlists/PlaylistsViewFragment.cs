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
    public class PlaylistsViewFragment : MainViewFragment, ActionMode.ICallback
    {
        private const int ProgressDelayMillis = 1200;

        private const string TagInputPlaylistNameDialog = "dialog_input_playlist_name";
        private const string TagRemovePlaylistsDialog = "dialog_remove_playlists";
        private const int RequestCodeEditPlaylist = 10;

        private const string ExtraIsInActionMode = "is_in_action_mode";
        private const string ExtraCheckedItemIds = "checked_item_ids";
        private const string ExtraPlaylistId = "playlist_id";
        private const string ExtraPlaylistIds = "playlist_ids";

        private ListView _listView;
        private View _progressPanel;
        private View _emptyIndicatorView;
        private PlaylistListAdapter _adapter;
        private ActionMode _actionMode;
        private bool _isInProgress;

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
            _listView.ChoiceMode = ChoiceMode.None;

            _progressPanel = view.FindViewById(Android.Resource.Id.Progress);
            _emptyIndicatorView = view.FindViewById(Android.Resource.Id.Empty);

            if (savedInstanceState != null)
            {
                if (savedInstanceState.ContainsKey(ExtraIsInActionMode))
                {
                    _actionMode = Activity.StartActionMode(this);

                    // Restore checked items in adapter when action mode is enabled
                    long[] checkedItemIds = savedInstanceState.GetLongArray(ExtraCheckedItemIds) ?? new long[0];
                    _adapter.SetCheckedItems(checkedItemIds);
                }
            }

            return view;
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            if (_actionMode != null)
            {
                outState.PutBoolean(ExtraIsInActionMode, true);
                outState.PutLongArray(ExtraCheckedItemIds, _adapter.GetCheckedItems());
            }
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
                    DisplayInputPlaylistNameDialog();
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
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

        public override void OnGenericPlaybackRequested()
        {
            SubmitRequest(new PlaySystemPlaylistsRequests());
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_playlists_view);
        }

        public override void OnResume()
        {
            base.OnResume();

            _listView.ItemClick += OnPlaylistClicked;
            _listView.ItemLongClick += OnPlaylistLongClicked;

            RegisterRequestResultHandler(typeof(LoadPlaylistsRequest), OnPlaylistsLoaded);
            RegisterRequestResultHandler(typeof(PlaySystemPlaylistsRequests), OnPlaySystemPlayslistsRequestFinished);
            RegisterRequestResultHandler(typeof(PlayAudioLibraryRequest), OnPlayAudioLibraryRequestFinished);

            var createPlaylistDialog = (InputTextDialogFragment) FragmentManager
                .FindFragmentByTag(TagInputPlaylistNameDialog);
            if (createPlaylistDialog != null)
            {
                createPlaylistDialog.AcceptClick += OnPlaylistNameEntered;
            }

            var removePlaylistsDialog = (ConfirmDialogFragment)FragmentManager
                .FindFragmentByTag(TagRemovePlaylistsDialog);
            if (removePlaylistsDialog != null)
            {
                removePlaylistsDialog.AcceptClick += OnPlaylistRemoveAccepted;
            }
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnPlaylistClicked;
            _listView.ItemLongClick -= OnPlaylistLongClicked;
            _listView.SetMultiChoiceModeListener(null);

            RemoveRequestResultHandler(typeof(LoadPlaylistsRequest));
            RemoveRequestResultHandler(typeof(PlaySystemPlaylistsRequests));
            RemoveRequestResultHandler(typeof(PlayAudioLibraryRequest));

            var createPlaylistDialog = (InputTextDialogFragment)FragmentManager
                .FindFragmentByTag(TagInputPlaylistNameDialog);
            if (createPlaylistDialog != null)
            {
                createPlaylistDialog.AcceptClick -= OnPlaylistNameEntered;
            }

            var removePlaylistsDialog = (ConfirmDialogFragment)FragmentManager
                .FindFragmentByTag(TagRemovePlaylistsDialog);
            if (removePlaylistsDialog != null)
            {
                removePlaylistsDialog.AcceptClick -= OnPlaylistRemoveAccepted;
            }

            base.OnPause();
        }

        private void DisplayRemovePlaylistsDialog(params long[] playlistIds)
        {
            if (playlistIds.Length == 0) return;

            var payload = new Bundle();
            payload.PutLongArray(ExtraPlaylistIds, playlistIds);

            string title = GetString(Resource.String.dialog_remove_playlists_title);
            string message = GetString(Resource.String.dialog_remove_playlists_message);
            message = string.Format(message, playlistIds.Length);

            var dialog = ConfirmDialogFragment.NewInstance(Activity, title, message, payload);

            dialog.AcceptClick += OnPlaylistRemoveAccepted;
            dialog.Show(FragmentManager, TagRemovePlaylistsDialog);
        }

        private void DisplayInputPlaylistNameDialog(string initialText = null, long? playlistId = null)
        {
            var payload = new Bundle();
            string title;
            if (playlistId == null)
            {
                title = GetString(Resource.String.dialog_create_playlist_title);
            }
            else
            {
                title = GetString(Resource.String.dialog_rename_playlist_title);
                payload.PutLong(ExtraPlaylistId, (long) playlistId);
            }
            string confirmText = GetString(Resource.String.dialog_create_playlist_confirm);
            var dialog = InputTextDialogFragment.CreateInputDialog(Activity, title, payload,
                confirmText, initalText: initialText);

            dialog.AcceptClick += OnPlaylistNameEntered;

            dialog.Show(FragmentManager, TagInputPlaylistNameDialog);
        }

        private void OnPlaylistRemoveAccepted(object sender, Bundle payload)
        {
            var playlistIds = payload.GetLongArray(ExtraPlaylistIds);

            if (PlaylistDao.RemovePlaylists(playlistIds) == false)
            {
                ShowMessage(Resource.String.error_cant_remove_playlists);
            }

            ReloadList();
        }

        private void OnPlaylistNameEntered(object sender, Bundle payload)
        {
            var dialog = (InputTextDialogFragment) sender;
            string text = dialog.InputText;

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowMessage(Resource.String.error_empty_playlist_name);
                return;
            }

            string textTrimmed = text.Trim();

            if (payload.ContainsKey(ExtraPlaylistId))
            {
                long playlistId = payload.GetLong(ExtraPlaylistId);

                if (PlaylistDao.RenamePlaylist(playlistId, textTrimmed) == false)
                {
                    ShowMessage(Resource.String.error_cant_rename_playlist);
                    AmwLog.Error(LogTag, string.Format(
                        "cant rename playlist (id:{0}) for name \"{1}\"", playlistId, textTrimmed));
                }
                else
                {
                    var item = _adapter.FindItem(playlistId);
                    if (item != null)
                    {
                        item.Name = textTrimmed;
                        _adapter.NotifyDataSetChanged();
                    }
                }

                return;
            }

            var playlistModel = PlaylistDao.CreateNewPlaylist(textTrimmed);
            if (playlistModel == null)
            {
                ShowMessage(Resource.String.error_cant_create_playlist);
                AmwLog.Error(LogTag, string.Format("cant create playlist for name \"{0}\"", textTrimmed));
            }
            else
            {
                // Delayed start to let finish animations
                App.MainHandler.PostDelayed(() => OpenPlaylistDetails(playlistModel.Id), 300);
            }
        }

        private void OnPlaylistLongClicked(object sender, AdapterView.ItemLongClickEventArgs args)
        {
            if (_actionMode != null) return;

            _listView.ChoiceMode = ChoiceMode.Multiple;
            _listView.SetItemChecked(args.Position, true);
            _adapter.ToggleItemCheck(args.Id);

            _actionMode = Activity.StartActionMode(this);
        }

        private void OnPlaylistClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            if (_actionMode != null)
            {
                _adapter.ToggleItemCheck(args.Id);
                _actionMode.Invalidate();   
            }
            else
            {
                long playlistId = _adapter[args.Position].Id;

                OpenPlaylistDetails(playlistId);
            }
        }

        private void OpenPlaylistDetails(long playlistId)
        {
            var fragmentArgs = new Bundle();
            fragmentArgs.PutLong(PlaylistDetailsFragment.ExtraPlaylistId, playlistId);

            var intent = FragmentContentActivity.CreateStartIntent(Activity,
                typeof(PlaylistDetailsFragment), fragmentArgs);

            Activity.StartActivityForResult(intent, RequestCodeEditPlaylist);
        }

        private void ReloadList()
        {
            _isInProgress = true;
            App.MainHandler.PostDelayed(UpdateProgressIndicators, ProgressDelayMillis);

            SubmitParallelRequest(new LoadPlaylistsRequest());
        }

        private void OnPlaylistsLoaded(object sender, ResultEventArgs args)
        {
            _isInProgress = false;
            if (args.Request.Status != RequestStatus.Ok)
            {
                UpdateProgressIndicators();
                AmwLog.Error(LogTag, "error loading playlists");
                return;
            }

            var playlists = ((LoadRequestResult<List<PlaylistModel>>) args.Result).Data;
           
            if (_adapter != null)
            {
                _adapter.SetItems(playlists);
            }
            UpdateProgressIndicators();
        }

        private void OnPlaySystemPlayslistsRequestFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                switch (args.Result.ResultCode)
                {
                    case PlaySystemPlaylistsRequests.ResultCodeErrorNoPlaylistsAvailable:
                        SubmitRequest(new PlayAudioLibraryRequest());
                        ShowMessage(Resource.String.error_cant_play_playlists_no_playlists);
                        break;

                    case PlaySystemPlaylistsRequests.ResultCodeErrorNoTracksAvailable:
                        SubmitRequest(new PlayAudioLibraryRequest());
                        ShowMessage(Resource.String.error_cant_play_playlists_no_audio);
                        break;

                    default:
                        ShowMessage(Resource.String.error_cant_play_playlists);
                        break;
                }
            }
        }

        private void OnPlayAudioLibraryRequestFinished(object sender, ResultEventArgs args)
        {
            AmwLog.Verbose(LogTag, "(playlists view) OnPlayAudioLibraryRequestFinished()");

            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "error trying to play audio library");
            }
        }

        private void UpdateProgressIndicators()
        {
            if (_listView == null || _listView.Count == 0)
            {
                if (_isInProgress)
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

        public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
        {
            var ids = _listView.GetCheckedItemIds();

            switch (item.ItemId)
            {
                case Resource.Id.ActionRemove:
                    DisplayRemovePlaylistsDialog(ids);
                    mode.Finish();
                    return true;

                case Resource.Id.ActionRename:
                    if (ids.Length == 1)
                    {
                        var playlistItem = _adapter.FindItem(ids[0]);
                        if (playlistItem != null)
                        {
                            DisplayInputPlaylistNameDialog(playlistItem.Name, ids[0]);
                            mode.Finish();

                            return true;
                        }
                    }
                    
                    ShowMessage(Resource.String.error_generic_cant_rename_playlist);
                    mode.Finish();
                    return true;

                default:
                    return false;
            }
        }

        public bool OnCreateActionMode(ActionMode mode, IMenu menu)
        {
            mode.MenuInflater.Inflate(Resource.Menu.context_menu_playlists_view, menu);

            return true;
        }

        public void OnDestroyActionMode(ActionMode mode)
        {
            _listView.ClearChoices();
            _listView.ChoiceMode = ChoiceMode.None;
            _adapter.ResetCheckedItems();
            _actionMode = null;
        }

        public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
        {
            int count = _listView.CheckedItemCount;

            IMenuItem item;
            switch (count)
            {
                case 0:
                    mode.Finish();
                    return true;

                case 1:
                    item = menu.FindItem(Resource.Id.ActionRename);
                    item.SetVisible(true);
                    return true;
            }

            item = menu.FindItem(Resource.Id.ActionRename);
            item.SetVisible(false);

            return true;
        }
    }
}