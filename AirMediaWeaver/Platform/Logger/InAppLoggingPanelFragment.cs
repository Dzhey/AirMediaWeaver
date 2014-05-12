using System;
using System.Globalization;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.UI.Base;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.Logger
{
    public class InAppLoggingPanelFragment : AmwFragment
    {
        private TextView _errorIndicatorView;
        private TextView _errorIndicatorTitleView;
        private TextView _warnIndicatorView;
        private TextView _warnIndicatorTitleView;
        private TextView _infoIndicatorView;
        private TextView _infoIndicatorTitleView;
        private TextView _indicatorEventTitle;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ResultListener.ShouldHandleAllRequests = true;
        }

        public override View OnCreateView(LayoutInflater inflater, 
            ViewGroup container, Bundle savedInstanceState)
        {
            var view = (ViewGroup) inflater.Inflate(Resource.Layout.View_InAppLogPanel, container, false);

            _errorIndicatorView = view.FindViewById<TextView>(Resource.Id.indicatorError);
            _errorIndicatorTitleView = view.FindViewById<TextView>(Resource.Id.indicatorErrorTitle);

            _warnIndicatorView = view.FindViewById<TextView>(Resource.Id.indicatorWarning);
            _warnIndicatorTitleView = view.FindViewById<TextView>(Resource.Id.indicatorWarningTitle);

            _infoIndicatorView = view.FindViewById<TextView>(Resource.Id.indicatorInfo);
            _infoIndicatorTitleView = view.FindViewById<TextView>(Resource.Id.indicatorInfoTitle);

            _indicatorEventTitle = view.FindViewById<TextView>(Resource.Id.indicatorEventTitle);
            _indicatorEventTitle.Text = "";

            view.Click += OnPanelClicked;

            return view;
        }

        public override void OnDestroyView()
        {
            View.Click -= OnPanelClicked;
            base.OnDestroyView();
        }

        public override void OnStart()
        {
            base.OnStart();

            RegisterRequestResultHandler(typeof(InsertLogEntriesRequest), OnInsertLogEntriesRequestFinished);
            RegisterRequestResultHandler(typeof(CountNewLogEntriesRequest), OnNewEntriesCountRequestFinished);
            ReloadNewLogEntriesCount();
        }

        public override void OnStop()
        {
            RemoveRequestResultHandler(typeof(InsertLogEntriesRequest));
            RemoveRequestResultHandler(typeof(CountNewLogEntriesRequest));

            base.OnStop();
        }

        private void ReloadNewLogEntriesCount()
        {
            SubmitParallelRequest(new CountNewLogEntriesRequest());
        }

        private void OnPanelClicked(object sender, EventArgs args)
        {
            var intent = new Intent(Activity, typeof(InAppLoggingActivity));
            Activity.StartActivity(intent);
        }

        private void OnNewEntriesCountRequestFinished(object sender, ResultEventArgs args)
        {
            var result = args.Result as CountNewLogEntriesRequest.RequestResult;

            if (result == null)
            {
                UpdateNewLogEntryIndicators(0, 0, 0);
                AmwLog.Error(LogTag, "error counting new log entries");
                return;
            }

            UpdateNewLogEntryIndicators(result.InfoEntriesCount, result.WarnEntriesCount, result.ErrorEntriesCount);
        }

        private void OnInsertLogEntriesRequestFinished(object sender, ResultEventArgs args)
        {
            var result = args.Result as InsertLogEntriesRequest.RequestResult;

            if (result == null || result.Entries.Length == 0) return;

            DisplayMessage(result.Entries[0]);
            ReloadNewLogEntriesCount();
        }

        private void DisplayMessage(LogEntryRecord record)
        {
            var color = LogUtils.GetLogColor(record.Level);

            _indicatorEventTitle.Text = record.Message;
            _indicatorEventTitle.SetTextColor(color);
        }

        private void UpdateNewLogEntryIndicators(int infoEntriesCount, int warnEntriesCount, int errorEntriesCount)
        {
            _errorIndicatorView.Text = errorEntriesCount.ToString(CultureInfo.CurrentUICulture);
            ApplyTextColor(errorEntriesCount, LogLevel.Error, _errorIndicatorTitleView, _errorIndicatorView);

            _warnIndicatorView.Text = warnEntriesCount.ToString(CultureInfo.CurrentUICulture);
            ApplyTextColor(warnEntriesCount, LogLevel.Warning, _warnIndicatorTitleView, _warnIndicatorView);

            _infoIndicatorView.Text = infoEntriesCount.ToString(CultureInfo.CurrentUICulture);
            ApplyTextColor(infoEntriesCount, LogLevel.Info, _infoIndicatorTitleView, _infoIndicatorView);
        }

        private void ApplyTextColor(int logEntryCount, LogLevel level, params TextView[] textViews)
        {
            Color textColor;

            if (logEntryCount > 0)
            {
                textColor = LogUtils.GetLogColor(level);
            }
            else
            {
                textColor = LogUtils.CreateColorFromResource(Resource.Color.log_color_verbose);
            }

            foreach (var textView in textViews)
            {
                textView.SetTextColor(textColor);
            }
        }
    }
}