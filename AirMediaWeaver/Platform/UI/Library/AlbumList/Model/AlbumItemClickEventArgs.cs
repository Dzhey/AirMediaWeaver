using System;

using Android.Views;

namespace AirMedia.Platform.UI.Library.AlbumList.Model
{
    public class AlbumItemClickEventArgs : EventArgs
    {
        public View ClickedView { get; set; }
        public AlbumGridItem Item { get; set; }

        public AlbumItemClickEventArgs()
        {
        }

        public AlbumItemClickEventArgs(View clickedView, AlbumGridItem item)
        {
            ClickedView = clickedView;
            Item = item;
        }
    }
}