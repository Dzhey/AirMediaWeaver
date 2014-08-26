using System;
using System.Collections.Generic;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public interface IAlbumsCoverProvider : IDisposable
    {
        event EventHandler<AlbumArtLoadedEventArgs> AlbumCoverLoaded;
        event EventHandler<AlbumArtsLoaderEnabledEventArgs> AlbumArtsLoaderEnabled;

        bool IsAlbumArtsLoaderEnabled { get; set; }

        Bitmap RequestAlbumCover(long albumId);
        void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts);
    }
}