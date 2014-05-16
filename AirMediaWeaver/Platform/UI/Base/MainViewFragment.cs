

using AirMedia.Platform.UI.Player;
using Android.OS;

namespace AirMedia.Platform.UI.Base
{
    public abstract class MainViewFragment : AmwFragment, IPlayerFacadeFragmentCallbacks
    {
        public abstract string GetTitle();

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            Activity.ActionBar.Title = GetTitle();
        }

        public abstract void OnGenericPlaybackRequested();
    }
}