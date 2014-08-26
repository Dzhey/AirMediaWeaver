using AirMedia.Platform.Controller.Requests.Interfaces;
using AirMedia.Platform.UI.Library.AlbumList.Adapter;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public interface IAlbumListContentWorker : IContextualRequestWorker
    {
        IAlbumListAdapter Adapter { get; }
        bool IsAlbumArtsLoaderEnabled { get; set; }
        AbsListView InflateContainerView(LayoutInflater inflater, ViewGroup root, bool attachToRoot);
    }
}