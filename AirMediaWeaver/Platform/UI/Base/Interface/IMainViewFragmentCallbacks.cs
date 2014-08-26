using AirMedia.Platform.UI.MainView;

namespace AirMedia.Platform.UI.Base.Interface
{
    public interface IMainViewFragmentCallbacks
    {
        void RequestNavigationTouchMode(MainMenuTouchMode mode);
        void RequestContentTitleUpdate(string contentTitle);
    }
}