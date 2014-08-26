using AirMedia.Platform.Controller.Requests.Interfaces;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public interface IAlbumListContentWorker : IContextualRequestWorker
    {
        IAlbumListAdapter Adapter { get; }
        bool IsAlbumArtsLoaderEnabled { get; set; }
    }
}