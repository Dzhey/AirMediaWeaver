using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public interface IAlbumGridAdapter
    {
        event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;
        event EventHandler<AlbumItemClickEventArgs> ItemClicked;
    }
}