

using System;
using AirMedia.Platform.UI.Player;
using AirMedia.Platform.UI.ViewUtils;
using Android.App;
using Android.OS;
using Android.Views;

namespace AirMedia.Platform.UI.Base
{
    public abstract class MainViewFragment : AmwFragment, 
        IPlayerFacadeFragmentCallbacks, 
        IProgressBarManagerCallbacks
    {

        private IMainViewFragmentCallbacks _callbacks;
        private ProgressBarManager _progressBarManager;

        public override void OnAttach(Activity activity)
        {
            base.OnAttach(activity);

            _callbacks = activity as IMainViewFragmentCallbacks;
            if (_callbacks == null)
            {
                throw new ApplicationException(string.Format(
                    "containing activity should implement {0} in order to use {1}",
                    typeof(IMainViewFragmentCallbacks), GetType()));
            }
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _progressBarManager = new ProgressBarManager(this);
        }

        public virtual void UpdateNavigationItems(ActionBar actionBar)
        {
        }

        public abstract string GetTitle();

        public virtual ActionBarNavigationMode GetNavigationMode()
        {
            return ActionBarNavigationMode.Standard;
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