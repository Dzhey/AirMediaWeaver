using System;
using AirMedia.Platform.UI.Library.AlbumList.Controller;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList.Adapter
{
    public interface IAlbumListAdapterCallbacks
    {
        AbsListView GetListView();
    }

    public interface IAlbumListAdapter : IListAdapter
    {
        IAlbumsCoverProvider AlbumsCoverProvider { get; set; }
        IAlbumListAdapterCallbacks Callbacks { get; set; }
        event EventHandler<AlbumItemClickEventArgs> ItemClicked;
        event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;
    }
}