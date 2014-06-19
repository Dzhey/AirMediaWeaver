using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Model;
using AirMedia.Core.Utils;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Logger;
using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumListGridAdapter : BaseAdapter<AlbumListEntry>, 
        AlbumGridItemsAdapter.ICallbacks, 
        LruCache<long, Bitmap>.ICacheEntryHandler
    {
        private class ViewHolder : Java.Lang.Object
        {
            public AlbumListEntry Item { get; set; }
            public AlbumGridItemsAdapter GridAdapter { get; set; }
            public TextView TitleView { get; set; }
            public GridView ItemsGrid { get; set; }
        }

        public static readonly string LogTag = typeof(AlbumListGridAdapter).Name;

        public const int MaxBitmapCacheSizeBytes = 1024 * 1024 * 8;

        public event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;

        private LruCache<long, Bitmap> _artCache;
        private readonly List<AlbumListEntry> _items;
        private readonly RequestFactory _loadArtRequestFactory;
        private readonly RequestResultListener _requestListener;
        private bool _isDisposed;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override AlbumListEntry this[int position]
        {
            get { return _items[position]; }
        }

        public AlbumListGridAdapter()
        {
            _items = new List<AlbumListEntry>();
            _requestListener = RequestResultListener.NewInstance(LogTag);
            _loadArtRequestFactory = AndroidRequestFactory.Init(typeof(LoadAlbumArtRequest), _requestListener)
                                                          .SetParallel(true)
                                                          .SetActionTag(LoadAlbumArtRequest.ActionTagDefault);
            _requestListener.RegisterResultHandler(typeof(LoadAlbumArtRequest), OnAlbumArtLoaded);
            _artCache = new LruCache<long, Bitmap>(MaxBitmapCacheSizeBytes, this);
        }

        public void SetItems(IEnumerable<AlbumListEntry> items)
        {
            _items.Clear();
            _items.AddRange(items);

            App.MainHandler.Post(NotifyDataSetChanged);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this[position];

            ViewHolder holder;

            if (convertView == null)
            {
                holder = new ViewHolder();
                convertView = LayoutInflater.From(parent.Context)
                                            .Inflate(Resource.Layout.View_AlbumListBlockEntry, parent, false);

                holder.TitleView = convertView.FindViewById<TextView>(Android.Resource.Id.Title);
                holder.ItemsGrid = convertView.FindViewById<GridView>(Resource.Id.albumsGrid);
                holder.GridAdapter = new AlbumGridItemsAdapter(holder.ItemsGrid, this);

                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder) convertView.Tag;
            }

            holder.Item = item;

            string artistName = string.IsNullOrEmpty(item.ArtistName)
                                    ? parent.Context.GetString(Resource.String.title_unknown_artist)
                                    : item.ArtistName;

            holder.TitleView.Text = artistName;
            holder.ItemsGrid.Adapter = holder.GridAdapter;

            holder.GridAdapter.SetItems(item.Albums);

            return convertView;
        }

        private void OnAlbumArtLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, "Error loading album art");
                return;
            }

            var bitmap = ((LoadRequestResult<Bitmap>)args.Result).Data;
            if (bitmap == null)
                return;

            var request = (LoadAlbumArtRequest) args.Request;
            long albumId = request.AlbumId;

            if (AlbumArtLoaded != null)
            {
                AlbumArtLoaded(this, new AlbumArtLoadedEventArgs
                    {
                        AlbumId = albumId,
                        AlbumArt = bitmap
                    });
            }

            _artCache.Set(albumId, bitmap, false);
        }

        public Bitmap GetAlbumArt(long albumId)
        {
            Bitmap albumArt = null;

            if (_artCache.TryGetValue(albumId, out albumArt) == false)
            {
                _loadArtRequestFactory.Submit(albumId);
            }

            return albumArt;
        }

        public int GetSizeOfValue(long key, Bitmap value)
        {
            if (value == null) return 0;

            return value.ByteCount;
        }

        public void DisposeOfValue(long key, Bitmap value)
        {
            value.Dispose();

            if (_isDisposed == false) return;

            value.Recycle();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _isDisposed = true;
                _artCache.Clear();
                _artCache = null;
            }
        }
    }
}