

using AirMedia.Platform.UI.Player;
using AirMedia.Platform.UI.ViewUtils;
using Android.OS;
using Android.Views;

namespace AirMedia.Platform.UI.Base
{
    public abstract class MainViewFragment : AmwFragment, 
        IPlayerFacadeFragmentCallbacks, 
        IProgressBarManagerCallbacks
    {
        private ProgressBarManager _progressBarManager;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _progressBarManager = new ProgressBarManager(this);
        }

        public abstract string GetTitle();

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            Activity.ActionBar.Title = GetTitle();
        }

        public abstract void OnGenericPlaybackRequested();
        public abstract bool HasDisplayedContent();

        protected void RegisterProgressPanel(ViewGroup progressPanel, 
            int appearanceDelayMillis, int emptyIndicatorTextResourceId)
        {
            _progressBarManager.RegisterViews(progressPanel);
            _progressBarManager.ProgressAppearanceDelayMillis = appearanceDelayMillis;

            if (emptyIndicatorTextResourceId != 0)
            {
                _progressBarManager.EmptyString = GetString(emptyIndicatorTextResourceId);
            }
        }

        protected void SetInProgress(bool isInProgress)
        {
            _progressBarManager.SetIsInProgress(isInProgress);
        }
    }
}