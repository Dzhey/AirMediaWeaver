using AirMedia.Platform.Controller.Requests.Interfaces;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public interface IAlbumListContentWorkerCallbacks : IContextualWorkerCallbacks
    {
        bool UserVisibleHint { get; }
        void ShowMessage(int stringResourceId);
        void OnContentDataLoaded(bool hasContentData);
    }
}