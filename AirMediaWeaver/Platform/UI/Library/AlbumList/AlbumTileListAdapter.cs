using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumTileListAdapter : BaseAdapter<AlbumGridItem>, IAlbumListAdapter
    {
        public IAlbumListAdapterCallbacks Callbacks { get; set; }
        public event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;
        public event EventHandler<AlbumItemClickEventArgs> ItemClicked;
        public event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;
        public bool IsAlbumArtsLoaderEnabled { get; set; }
        public void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts)
        {
            throw new NotImplementedException();
        }

        public void SetItems(IEnumerable<AlbumListEntry> data)
        {
            throw new NotImplementedException();
        }

        public override long GetItemId(int position)
        {
            throw new NotImplementedException();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            throw new NotImplementedException();
        }

        public override int Count
        {
            get { throw new NotImplementedException(); }
        }

        public override AlbumGridItem this[int position]
        {
            get { throw new NotImplementedException(); }
        }
    }
}