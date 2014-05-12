using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform.UI;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Logger
{
    public class InAppLoggingFragment : AmwFragment
    {
        private const string TagLogDetailsDialog = "log_details_dialog";
        private const string TagProgressDialog = "InAppLoggingFragment_progress_dialog";
        private const string TagInfoDialog = "InAppLoggingFragment_info_dialog";

        private const string ExtraIsLogRefreshEnabled = "is_log_refresh_enabled";

        private const string ActionTagSaveLogFile = "save_log_file";
        private const string ActionTagSaveLogFileAndShare = "save_log_file_and_share";

        private Spinner _logLevelSpinner;
        private ListView _logListView;
        private Switch _listDisplaySwitch;
        private ImageButton _buttonSaveLog;
        private ImageButton _buttonShareLog;
        private ToggleButton _logRefreshToggle;
        private ProgressBar _topBarProgressIndicator;
        private TextView _textViewEmpty;
        private TextView _logEntryCountIndicator;
        private View _progressPanel;
        private LogLevelAdapter _spinnerAdapter;
        private LogEntryListAdapter _adapter;
        private bool _isLogRefreshEnabled = true;
        private int _reloadCount;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _spinnerAdapter = new LogLevelAdapter();

            var items = new LinkedList<LogLevelItem>();
            foreach (var item in Enum.GetValues(typeof(LogLevel)))
            {
                var level = (LogLevel) item;

                if (level == LogLevel.None) continue;

                items.AddFirst(new LogLevelItem { Level = level });
            }
            _spinnerAdapter.SetItems(items);

            ResultListener.ShouldHandleAllRequests = true;
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = (ViewGroup) inflater.Inflate(
                Resource.Layout.Fragment_InAppLogging, container, false);

            _buttonSaveLog = view.FindViewById<ImageButton>(Resource.Id.buttonSave);
            _buttonShareLog = view.FindViewById<ImageButton>(Resource.Id.buttonShare);

            _textViewEmpty = view.FindViewById<TextView>(global::Android.Resource.Id.Empty);
            _progressPanel = view.FindViewById(Resource.Id.progressPanel);
            _logRefreshToggle = view.FindViewById<ToggleButton>(Resource.Id.toggleLogRefreshButton);
            _logEntryCountIndicator = view.FindViewById<TextView>(Resource.Id.logEntryCountIndicator);
            _topBarProgressIndicator = view.FindViewById<ProgressBar>(Resource.Id.topBarProgressIndicator);

            _logLevelSpinner = view.FindViewById<Spinner>(Resource.Id.logLevelSpinner);
            _logLevelSpinner.Adapter = _spinnerAdapter;

            _listDisplaySwitch = view.FindViewById<Switch>(Resource.Id.listDisplaySwitch);

            _logListView = view.FindViewById<ListView>(global::Android.Resource.Id.List);
            _adapter = new LogEntryListAdapter(_logListView);
            _adapter.ItemDetailsClicked += OnButtonDetailsClicked;
            _adapter.IsAllItemsExpanded = App.Preferences.IsLogListExpanded;
            _logListView.Adapter = _adapter;

            _listDisplaySwitch.Checked = App.Preferences.IsLogListExpanded;

            var level = App.Preferences.DisplayLogLevel;
            int pos = _spinnerAdapter.FindItemPosition(level);
            if (pos > 0)
            {
                _logLevelSpinner.SetSelection(pos);
            }

            if (savedInstanceState != null)
            {
                _isLogRefreshEnabled = savedInstanceState.GetBoolean(ExtraIsLogRefreshEnabled, false);
            }

            _logRefreshToggle.Checked = !_isLogRefreshEnabled;

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            ReloadItems();
            UpdateNotificationViews();
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutBoolean(ExtraIsLogRefreshEnabled, _isLogRefreshEnabled);
        }

        public override void OnStart()
        {
            base.OnStart();

            RegisterRequestResultHandler(typeof(LoadLogEntriesRequest), OnLogEntriesLoaded);
            RegisterRequestResultHandler(typeof(InsertLogEntriesRequest), OnLogEntriesInserted);
            RegisterRequestResultHandler(typeof(SaveLogFileRequest), OnLogFileSaved);

            _buttonSaveLog.Enabled = true;
            _buttonShareLog.Enabled = true;

            _logRefreshToggle.CheckedChange += OnRefreshStateSwitched;
            _listDisplaySwitch.CheckedChange += OnListDisplaySwitched;
            _logLevelSpinner.ItemSelected += OnLogLevelSelected;
            _logListView.ItemClick += OnItemClicked;
            _buttonSaveLog.Click += OnButtonSaveClicked;
            _buttonShareLog.Click += OnButtonShareClicked;
        }

        public override void OnStop()
        {
            _logListView.ItemClick -= OnItemClicked;
            _logLevelSpinner.ItemSelected -= OnLogLevelSelected;
            _listDisplaySwitch.CheckedChange -= OnListDisplaySwitched;
            _buttonSaveLog.Click -= OnButtonSaveClicked;
            _buttonShareLog.Click -= OnButtonShareClicked;
            _adapter.ItemDetailsClicked -= OnButtonDetailsClicked;
            _logRefreshToggle.CheckedChange -= OnRefreshStateSwitched;

            RemoveRequestResultHandler(typeof(LoadLogEntriesRequest));
            RemoveRequestResultHandler(typeof(InsertLogEntriesRequest));
            RemoveRequestResultHandler(typeof(SaveLogFileRequest));

            base.OnStop();
        }

        private void OnButtonDetailsClicked(object sender, LogEntryRecord item)
        {
            string title = GetString(Resource.String.dialog_log_details_title);
            string message = string.IsNullOrEmpty(item.Details) ? "<no details>" : item.Details;
            string acceptText = GetString(Resource.String.dialog_log_details_accept_text);
            var fragment = ConfirmDialogFragment.NewInstance(Activity, title, message,
                                                             iconId: global::Android.Resource.Drawable.IcDialogInfo,
                                                             displayCancelButton: false,
                                                             acceptText: acceptText);
            fragment.Show(FragmentManager, TagLogDetailsDialog);
        }

        private int? SubmitSaveLogFileRequest(string actionTag)
        {
            _buttonSaveLog.Enabled = false;
            _buttonShareLog.Enabled = false;

            var outputDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads);

            if (outputDir == null)
            {
                ShowMessage(Resource.String.error_log_dir_not_available);
                return null;
            }

            var rq = new SaveLogFileRequest(outputDir.AbsolutePath) {ActionTag = actionTag};
            return SubmitParallelRequest(rq);
        }

        private void OnButtonSaveClicked(object sender, EventArgs args)
        {
            int? requestId = SubmitSaveLogFileRequest(ActionTagSaveLogFile);
            if (requestId != null)
            {
                DisplayProgressDialog((int)requestId);
            }
        }

        private void OnButtonShareClicked(object sender, EventArgs args)
        {
            int? requestId = SubmitSaveLogFileRequest(ActionTagSaveLogFileAndShare);
            if (requestId != null)
            {
                DisplayProgressDialog((int) requestId);
            }
        }

        private void OnRefreshStateSwitched(object sender, CompoundButton.CheckedChangeEventArgs args)
        {
            _isLogRefreshEnabled = !args.IsChecked;

            if (_isLogRefreshEnabled)
            {
                ReloadItems();
            }
        }

        private void OnListDisplaySwitched(object sender, CompoundButton.CheckedChangeEventArgs args)
        {
            if (_adapter != null)
            {
                _adapter.IsAllItemsExpanded = args.IsChecked;
            }
            App.Preferences.IsLogListExpanded = args.IsChecked;
        }

        private void OnLogLevelSelected(object sender, AdapterView.ItemSelectedEventArgs args)
        {
            var item = _spinnerAdapter[args.Position];

            App.Preferences.DisplayLogLevel = item.Level;

            _reloadCount = 0;
            ReloadItems();
        }

        private void OnItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            _adapter.ToggleItem((int) args.Id);
        }

        private void ReloadItems()
        {
            if (_reloadCount > 0 && _isLogRefreshEnabled == false) return;

            _reloadCount++;

            var level = App.Preferences.DisplayLogLevel;

            SubmitParallelRequest(new LoadLogEntriesRequest(level));
        }

        private void OnLogEntriesLoaded(object sender, ResultEventArgs args)
        {
            var result = args.Result as LoadLogEntriesRequest.RequestResult;

            if (result == null)
            {
                ShowMessage("Error: can't load logs");
                UpdateNotificationViews();

                return;
            }

            if (_isLogRefreshEnabled || (_reloadCount <= 1))
            {
                _adapter.SetItems(result.Data);
                _logEntryCountIndicator.Text = string.Format("{0}/{1}", result.LevelEntryCount, result.TotalEntryCount);

                // Update last read log entry id
                if (result.Data.Count > 0)
                {
                    App.Preferences.LastReadLogEntryId = result.Data[0].Id;
                }
            }

            _reloadCount++;
            UpdateNotificationViews();
        }

        private void DisplayProgressDialog(int requestId)
        {
            string title = GetString(Resource.String.dialog_save_file_title);
            var dialog = ProgressDialogFragment.NewInstance(title, requestId);

            dialog.Show(FragmentManager, TagProgressDialog);
        }

        private void OnLogEntriesInserted(object sender, ResultEventArgs args)
        {
            ReloadItems();
            UpdateNotificationViews();
        }

        private void OnLogFileSaved(object sender, ResultEventArgs args)
        {
            _buttonSaveLog.Enabled = true;
            _buttonShareLog.Enabled = true;

            var result = args.Result as SaveLogFileRequest.RequestResult;

            if (result == null)
            {
                ShowMessage(Resource.String.error_cant_save_log_file);

                return;
            }

            var request = (SaveLogFileRequest) args.Request;

            switch (result.ResultCode)
            {
                case RequestResult.ResultCodeOk:
                    if (request.ActionTag == ActionTagSaveLogFile)
                    {
                        DisplayLogFileInfoFragment(result.FilePath, request.FileName);
                    } 
                    else if (request.ActionTag == ActionTagSaveLogFileAndShare)
                    {
                        SendShareLogFileIntent(result.FilePath);
                    }
                    break;

                case SaveLogFileRequest.RequestResult.ResultCodeErrorCantCreateFile:
                    ShowMessage(Resource.String.error_cant_create_log_file);
                    break;

                default:
                    ShowMessage(Resource.String.error_cant_save_log_file);
                    break;
            }
        }

        private void SendShareLogFileIntent(string filePath)
        {
            string message = GetString(Resource.String.dialog_title_send_log_via);
            var intent = new Intent(Intent.ActionSend);
            var uri = Uri.Parse(ContentResolver.SchemeFile + "://" + filePath);
            intent.SetType("text/plain");
            intent.PutExtra(Intent.ExtraStream, uri);
            Activity.StartActivity(Intent.CreateChooser(intent, message));
            AmwLog.Verbose(LogTag, string.Format("start activity to send \"{0}\"", uri));
        }

        private void DisplayLogFileInfoFragment(string filePath, string fileName)
        {
            string acceptText = GetString(global::Android.Resource.String.Ok);
            string message = GetString(Resource.String.message_log_file_saved);
            message = message.HaackFormat(new { file_path = filePath });
            var fragment = ConfirmDialogFragment.NewInstance(Activity, fileName, message,
                                                             acceptText: acceptText, displayCancelButton: false,
                                                             iconId: global::Android.Resource.Drawable.IcDialogInfo);

            fragment.Show(FragmentManager, TagInfoDialog);
        }

        private void UpdateNotificationViews()
        {
            if (_adapter == null)
            {
                _textViewEmpty.Visibility = ViewStates.Gone;
                _progressPanel.Visibility = ViewStates.Gone;

                return;
            }

            // Display text view with empty list notification if needed
            if (_textViewEmpty != null)
            {
                if (HasPendingRequests == false)
                {
                    _textViewEmpty.Visibility = _adapter.IsEmpty ? ViewStates.Visible : ViewStates.Gone;
                }
                else
                {
                    _textViewEmpty.Visibility = ViewStates.Gone;
                }
            }

            // Display progress bar if needed
            if (_progressPanel != null)
            {
                if (HasPendingRequests)
                {
                    _progressPanel.Visibility = _adapter.IsEmpty ? ViewStates.Visible : ViewStates.Gone;
                    _topBarProgressIndicator.Visibility = ViewStates.Visible;
                }
                else
                {
                    _progressPanel.Visibility = ViewStates.Gone;
                    _topBarProgressIndicator.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}