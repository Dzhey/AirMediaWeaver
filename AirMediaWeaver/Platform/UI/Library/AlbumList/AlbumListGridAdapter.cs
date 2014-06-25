using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Impl;
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

        public interface ICallbacks
        {
            void OnLowMemoryDetected();
        }

        public static readonly string LogTag = typeof(AlbumListGridAdapter).Name;

        public const int MaxBitmapCacheSizeBytes = 1024 * 1024 * 8;

        private const string BatchLoadArtsRequestTag = "load_album_arts_batch_request";

        public event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;
        public event EventHandler<AlbumGridItem> ItemClicked;

        private LruCache<long, Bitmap> _artCache;
        private readonly List<AlbumListEntry> _items;
        private readonly IRequestFactory _loadArtRequestFactory;
        private readonly RequestResultListener _requestListener;
        private ICallbacks _callbacks;
        private bool _isDisposed;
        private bool _isLowMemory;
        private RequestManager _albumArtsLoader;

        public override int Count
        {
            get { return _items.Count; }
        }

        public override AlbumListEntry this[int position]
        {
            get { return _items[position]; }
        }

        public AlbumListGridAdapter(ICallbacks callbacks)
        {
            _callbacks = callbacks;
            _items = new List<AlbumListEntry>();
            _albumArtsLoader = new ThreadPoolRequestManager(2);
            _requestListener = RequestResultListener.NewInstance(LogTag, _albumArtsLoader);

            var factory = BatchRequestFactory.Init(typeof (LoadAlbumArtRequest), typeof(LoadAlbumArtBatchRequest));
            _loadArtRequestFactory = AndroidRequestFactory.Init(factory, _requestListener)
                                                          .SetManager(_albumArtsLoader)
                                                          .SetParallel(true)
                                                          .SetActionTag(BatchLoadArtsRequestTag);

            _requestListener.RegisterResultHandler(typeof(BatchRequest), OnAlbumArtLoaded);
            _artCache = new LruCache<long, Bitmap>(MaxBitmapCacheSizeBytes, this);
        }

        public void SetItems(IEnumerable<AlbumListEntry> items)
        {
            _items.Clear();
            _items.AddRange(items);

            App.MainHandler.Post(NotifyDataSetChanged);
        }

        public void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts)
        {
            foreach (var entry in albumArts)
            {
                _artCache.Set(entry.Key, entry.Value);
            }
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
                holder.ItemsGrid.ItemClick += OnGridItemClicked;

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

        private void OnGridItemClicked(object sender, AdapterView.ItemClickEventArgs itemClickEventArgs)
        {
            if (ItemClicked == null) return;

            var senderTyped = (GridView) sender;
            var item = ((AlbumGridItemsAdapter) senderTyped.Adapter)[itemClickEventArgs.Position];

            ItemClicked(this, item);
        }

        private void OnAlbumArtLoaded(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, "Error loading album arts");
                return;
            }

            var results = ((BatchRequestResult) args.Result).Results;

            foreach (var result in results)
            {
                var rq = (LoadAlbumArtRequest) result.Key;

                if (rq.Status != RequestStatus.Ok)
                {
                    var ex = rq.Result.RisenException;
                    if (ex != null
                        && string.IsNullOrEmpty(ex.Message) == false
                        && ex.Message.Contains("OutOfMemory"))
                    {
                        _artCache.Clear();
                        _isLowMemory = true;
                        AmwLog.Warn(LogTag, "Out of Memory detected");
                        _callbacks.OnLowMemoryDetected();
                        break;
                    }
                    continue;
                }

                long albumId = rq.AlbumId;
                var bitmap = ((LoadRequestResult<Bitmap>)result.Value).Data;
                OnAlbumArtLoaded(albumId, bitmap);       
            }

            _albumArtsLoader.TryDisposeRequest(args.Request.RequestId);
        }

        private void OnAlbumArtLoaded(long albumId, Bitmap albumArt)
        {
            if (AlbumArtLoaded != null)
            {
                AlbumArtLoaded(this, new AlbumArtLoadedEventArgs
                {
                    AlbumId = albumId,
                    AlbumArt = albumArt
                });
            }

            _artCache.Set(albumId, albumArt, false);
        }

        public Bitmap GetAlbumArt(long albumId)
        {
            Bitmap albumArt = null;

            if (_isLowMemory == false && _artCache.TryGetValue(albumId, out albumArt) == false)
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

        public void DisposeOfValue(long keyAlbumId, Bitmap albumArt)
        {
            if (albumArt != null)
                albumArt.Dispose();

            if (_isDisposed == false) return;

            if (albumArt != null)
                albumArt.Recycle();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            base.Dispose(disposing);

            if (disposing)
            {
                _requestListener.Dispose();
                _artCache.Clear();
                _artCache = null;
                _albumArtsLoader.Dispose();
                _albumArtsLoader = null;
                _callbacks = null;
            }

            _isDisposed = true;
        }
    }
}