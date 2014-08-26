using System;
using System.Collections.Generic;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public interface IAlbumListAdapterCallbacks
    {
        AbsListView GetListView();
    }

    public interface IAlbumListAdapter : IListAdapter
    {
        IAlbumListAdapterCallbacks Callbacks { get; set; }
        event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;
        event EventHandler<AlbumItemClickEventArgs> ItemClicked;
        event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;
        bool IsAlbumArtsLoaderEnabled { get; set; }
        void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts);
        void SetItems(IEnumerable<AlbumListEntry> data);
    }
}