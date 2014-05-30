using System.Globalization;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Player;
using AirMedia.Platform.UI.Base;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.UI.Library
{
    public class AudioLibraryFragment : MainViewFragment, 
        LoaderManager.ILoaderCallbacks, 
        ActionMode.ICallback, 
        ITrackListAdapterCallbacks
    {
        public const string ExtraStartInPickMode = "start_in_pick_mode";
        public const string ExtraCheckedTrackIds = "checked_track_ids";

        private const int TrackListLoaderId = 1;

        private ListView _listView;
        private TrackListCursorAdapter _adapter;
        private bool _isInPickMode;
        private ActionMode _actionMode;
        private long[] _checkedItems;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Arguments != null)
            {
                _isInPickMode = Arguments.GetBoolean(ExtraStartInPickMode, false);

                if (savedInstanceState == null && Arguments.ContainsKey(ExtraCheckedTrackIds))
                {
                    _checkedItems = Arguments.GetLongArray(ExtraCheckedTrackIds);
                }
            }

            if (savedInstanceState != null)
            {
                if (savedInstanceState.ContainsKey(ExtraCheckedTrackIds))
                {
                    _checkedItems = savedInstanceState.GetLongArray(ExtraCheckedTrackIds);
                }
            }
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Fragment_AudioLibrary, container, false);

            _listView = view.FindViewById<ListView>(Android.Resource.Id.List);
            var progresPanel = view.FindViewById<ViewGroup>(Resource.Id.progressPanel);
            RegisterProgressPanel(progresPanel, Consts.DefaultProgressDelayMillis, 
                Resource.String.note_audio_library_empty);

            if (_adapter != null)
            {
                _listView.Adapter = _adapter;
            }

            if (_isInPickMode)
            {
                _actionMode = Activity.StartActionMode(this);
                _listView.ChoiceMode = ChoiceMode.Multiple;
            }

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            SetInProgress(true);
            LoaderManager.InitLoader(TrackListLoaderId, null, this);
        }

        public override void OnGenericPlaybackRequested()
        {
            SubmitRequest(new PlayAudioLibraryRequest());
        }

        public override bool HasDisplayedContent()
        {
            return _listView != null && _listView.Count > 0;
        }

        private void OnPlayAudioLibraryRequestFinished(object sender, ResultEventArgs args)
        {
            AmwLog.Verbose(LogTag, "(audio library) OnPlayAudioLibraryRequestFinished()");

            if (args.Request.Status != RequestStatus.Ok)
            {
                switch (args.Result.ResultCode)
                {
                    case PlayAudioLibraryRequest.ResultCodeErrorNoAvailableTracks:
                        ShowMessage(Resource.String.error_cant_start_audio_library_playback_no_audio);
                        break;

                    default:
                        ShowMessage(Resource.String.error_cant_start_audio_library_playback);
                        AmwLog.Error(LogTag, "unexpected error while trying to start audio library playback");
                        break;
                }
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            if (_actionMode != null)
            {
                outState.PutLongArray(ExtraCheckedTrackIds, _listView.GetCheckItemIds());
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterRequestResultHandler(typeof(PlayAudioLibraryRequest), OnPlayAudioLibraryRequestFinished);

            _listView.ItemClick += OnTrackItemClicked;
        }

        public override void OnPause()
        {
            _listView.ItemClick -= OnTrackItemClicked;

            RemoveRequestResultHandler(typeof(PlayAudioLibraryRequest));

            base.OnPause();
        }

        public override string GetTitle()
        {
            return GetString(Resource.String.title_audio_library);
        }

        public Loader OnCreateLoader(int id, Bundle args)
        {
            switch (id)
            {
                case TrackListLoaderId:
                    return new TrackListLoader(Activity);

                default:
                    AmwLog.Error(LogTag, string.Format("undefined loader \"{0}\" requested", id));
                    return null;
            }
        }

        public void OnLoaderReset(Loader loader)
        {
            _listView.Adapter = null;
            _adapter = null;
        }

        public void OnLoadFinished(Loader loader, Object data)
        {
            var cursor = data as ICursor;

            if (cursor == null)
            {
                // 'Release' build type cast problem workaround
// ReSharper disable ExpressionIsAlwaysNull
                cursor = data as CursorWrapper;
// ReSharper restore ExpressionIsAlwaysNull
            }

            if (cursor == null)
            {
                AmwLog.Error(LogTag, string.Format(
                    "failed to load audio library: can't cast cursor; cursor data: \"{0}\"", data));
                ShowMessage(Resource.String.error_cant_load_audiolibrary);
                SetInProgress(false);
                return;
            }

            _adapter = new TrackListCursorAdapter(Activity, this, cursor);
            _adapter.ShouldDisplayCheckboxes = _isInPickMode;

            if (_listView != null)
            {
                _listView.Adapter = _adapter;
                _listView.Post(() => _adapter.NotifyDataSetChanged());

                // Set initial track selection
                if (_actionMode != null && _checkedItems != null && _checkedItems.Length > 0)
                {
                    App.MainHandler.Post(delegate
                        {
                            SetListChecks(_checkedItems);
                            _actionMode.Invalidate();
                            _checkedItems = null;
                        });
                }
            }

            SetInProgress(false);
        }

        private void SetListChecks(long[] checkedTrackIds)
        {
            if (_listView == null || _adapter == null || checkedTrackIds == null) return;

            _listView.ClearChoices();

            foreach (var id in checkedTrackIds)
            {
                int position = _adapter.FindItemPosition(id);
                if (position != -1)
                {
                    _listView.SetItemChecked(position, true);
                }
            }

            _adapter.NotifyDataSetChanged();
        }

        private void OnTrackItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            if (_actionMode != null)
            {
                _actionMode.Invalidate();
                _adapter.NotifyDataSetChanged();
                return;
            }

            int position = args.Position;
            long[] trackIds = _adapter.GetDisplayedTrackIds();

            PlayerControl.Play(trackIds, position);
        }

        public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
        {
            return false;
        }

        public bool OnCreateActionMode(ActionMode mode, IMenu menu)
        {
            return true;
        }

        public void OnDestroyActionMode(ActionMode mode)
        {
            var checkedItemIds = _listView.GetCheckItemIds();

            _listView.ClearChoices();
            _listView.ChoiceMode = ChoiceMode.None;
            _actionMode = null;

            if (_isInPickMode)
            {
                var data = new Intent().PutExtra(ExtraCheckedTrackIds, checkedItemIds);
                Activity.SetResult(Result.Ok, data);
                Activity.Finish();
            }
        }

        public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
        {
            int count = _listView.CheckedItemCount;

            if (count == 0)
            {
                mode.SetTitle(Resource.String.hint_pick_playlist_tracks);
            }
            else
            {
                mode.Title = count.ToString(CultureInfo.CurrentUICulture);
            }

            return true;
        }

        public bool IsItemChecked(int position)
        {
            if (_listView == null) return false;

            return _listView.IsItemChecked(position);
        }
    }
}