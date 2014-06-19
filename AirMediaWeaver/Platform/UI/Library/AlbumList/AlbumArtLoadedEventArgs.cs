using System;

using Android.Graphics;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumArtLoadedEventArgs : EventArgs
    {
        public Bitmap AlbumArt { get; set; }
        public long AlbumId { get; set; }
    }
}