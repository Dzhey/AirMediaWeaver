

using AirMedia.Core.Log;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.ViewUtils
{
    public class ProgressBarManager
    {
        public static readonly string LogTag = typeof (ProgressBarManager).Name;

        public int ProgressAppearanceDelayMillis { get; set; }
        public string EmptyString
        {
            get
            {
                return _emptyString;
            }
            set
            {
                _emptyString = value;
                UpdateEmptyIndicatorText();
            }
        }

        private readonly IProgressBarManagerCallbacks _callbacks;
        private bool _isInProgress;
        private View _progressPanel;
        private TextView _emptyIndicatorView;
        private string _emptyString;

        public ProgressBarManager(IProgressBarManagerCallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        public void RegisterViews(ViewGroup container)
        {
            _progressPanel = container.FindViewById(Android.Resource.Id.Progress);
            _emptyIndicatorView = container.FindViewById<TextView>(Android.Resource.Id.Empty);

            if (_progressPanel == null || _emptyIndicatorView == null)
            {
                AmwLog.Error(LogTag, string.Format("{0} can't fetch progress " +
                                                   "view components", GetType().Name));
            }
            else
            {
                _progressPanel.Visibility = ViewStates.Gone;
                _emptyIndicatorView.Visibility = ViewStates.Gone;
                UpdateEmptyIndicatorText();
            }
        }

        public void SetIsInProgress(bool isInProgress)
        {
            _isInProgress = isInProgress;

            if (_isInProgress && ProgressAppearanceDelayMillis > 0)
            {
                App.MainHandler.PostDelayed(UpdateProgressIndicators, ProgressAppearanceDelayMillis);
            }
            else
            {
                UpdateProgressIndicators();
            }
        }

        private void UpdateEmptyIndicatorText()
        {
            if (_emptyIndicatorView == null) return;
            
            if (EmptyString == null)
            {
                _emptyIndicatorView.SetText(Resource.String.note_content_view_empty);
            }
            else
            {
                _emptyIndicatorView.Text = EmptyString;
            }
        }

        private void UpdateProgressIndicators()
        {
            if (_progressPanel == null || _emptyIndicatorView == null) return;

            if (_callbacks.HasDisplayedContent() == false)
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
    }
}