using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.Requests.Interfaces;
using AirMedia.Platform.Logger;
using AirMedia.Platform.UI.Library.AlbumList.Controller;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using AirMedia.Platform.Util;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Java.Lang;
using EntryDisposedEventArgs = AirMedia.Platform.Util.LruReuseReuseBitmapCache<long>.EntryDisposedEventArgs;

namespace AirMedia.Platform.UI.Library.AlbumList
{
    public class AlbumListGridAdapter : BaseAdapter<AlbumListEntry>,
        AlbumGridItemsAdapter.ICallbacks, IAlbumListAdapter
    {
        private class ViewHolder : Java.Lang.Object
        {
            public AlbumListEntry Item { get; set; }
            public AlbumGridItemsAdapter GridAdapter { get; set; }
            public TextView TitleView { get; set; }
            public GridView ItemsGrid { get; set; }
        }

        public static readonly string LogTag = typeof(AlbumListGridAdapter).Name;

        public const int MaxBitmapCacheSizeBytes = 1024 * 1024 * 16;
        private const int AlbumArtsLoaderBatchPeriodMillis = 100;

        private const string BatchLoadArtsRequestTag = "load_album_arts_batch_request";
        private const int ArtsLoaderThreadPoolSize = 2;

        public event EventHandler<AlbumArtLoadedEventArgs> AlbumArtLoaded;
        public event EventHandler<AlbumItemClickEventArgs> ItemClicked;
        public event EventHandler<AlbumItemClickEventArgs> ItemMenuClicked;

        public bool IsResultHandlerDisabled { get; set; }

        public IAlbumListAdapterCallbacks Callbacks
        {
            get
            {
                if (_callbacks == null)
                {
                    throw new IllegalStateException("album adapter callbacks is not defined");
                }

                return _callbacks;
            }

            set { _callbacks = value; }
        }

        public bool IsAlbumArtsLoaderEnabled
        {
            get
            {
                return _isAlbumArtsLoaderEnabled;
            }

            set
            {
                _isAlbumArtsLoaderEnabled = value;
                if (_isAlbumArtsLoaderEnabled)
                    UpdateVisibleAlbumArts();
            }
        }

        private readonly List<AlbumListEntry> _items;
        private readonly IRequestFactory _loadArtRequestFactory;
        private readonly RequestResultListener _requestListener;
        private IAlbumListAdapterCallbacks _callbacks;
        private bool _isDisposed;
        private bool _isLowMemory;
        private bool _isAlbumArtsLoaderEnabled = true;
        private RequestManager _albumArtsLoader;
        private readonly ISet<long> _requestedAlbumArts;
        private readonly LruReuseReuseBitmapCache<long> _artsCache;

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
            _requestedAlbumArts = new HashSet<long>();
            _albumArtsLoader = new ThreadPoolRequestManager(new AndroidThreadPoolWorker(ArtsLoaderThreadPoolSize));
            _requestListener = RequestResultListener.NewInstance(LogTag, _albumArtsLoader);

            var factory = BatchRequestFactory.Init(typeof (LoadAlbumArtRequest), typeof(LoadAlbumArtBatchRequest));
            factory.FlushTimeoutMillis = AlbumArtsLoaderBatchPeriodMillis;

            _loadArtRequestFactory = AndroidRequestFactory.Init(factory, _requestListener)
                                                          .SetManager(_albumArtsLoader)
                                                          .SetParallel(true)
                                                          .SetActionTag(BatchLoadArtsRequestTag);

            _requestListener.RegisterResultHandler(typeof(BatchRequest), OnAlbumArtLoaded);
            _artsCache = new LruReuseReuseBitmapCache<long>(MaxBitmapCacheSizeBytes);
            _artsCache.EntryDisposed += OnAlbumArtDisposed;
        }

        public void SetItems(IEnumerable<AlbumListEntry> items)
        {
            if (_isDisposed) return;

            _items.Clear();
            _items.AddRange(items);

            App.MainHandler.Post(NotifyDataSetChanged);
        }

        public void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts)
        {
            if (_isDisposed) return;

            foreach (var entry in albumArts)
            {
                _artsCache.Set(entry.Key, entry.Value, false);
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        private int _viewCount = 0;
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
                holder.GridAdapter.ItemMenuClicked += OnGridItemMenuClicked;
                holder.GridAdapter.ItemClicked += OnGridItemClicked;

                holder.ItemsGrid.Adapter = holder.GridAdapter;

                convertView.Tag = holder;
                _viewCount++;
                AmwLog.Info(LogTag, "grid created: " + _viewCount);
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }

            holder.Item = item;

            string artistName = string.IsNullOrEmpty(item.ArtistName)
                                    ? parent.Context.GetString(Resource.String.title_unknown_artist)
                                    : item.ArtistName;

            holder.TitleView.Text = artistName.ToUpper();

            holder.GridAdapter.SetItems(item.Albums);

            return convertView;
        }

        private void UpdateVisibleAlbumArts()
        {
            var listView = Callbacks.GetListView();

            if (listView == null)
                return;

            for (int i = listView.FirstVisiblePosition, pos = 0; i <= listView.LastVisiblePosition; i++, pos++)
            {
                var holder = listView.GetChildAt(pos).Tag as ViewHolder;

                if (holder == null)
                    continue;

                holder.GridAdapter.UpdateVisibleAlbumArts();
            }
        }

        private void OnGridItemClicked(object sender, AlbumItemClickEventArgs args)
        {
            if (ItemClicked == null) return;

            ItemClicked(this, args);
        }

        private void OnGridItemMenuClicked(object sender, AlbumItemClickEventArgs args)
        {
            if (ItemMenuClicked == null) return;

            ItemMenuClicked(this, args);
        }

        private void OnAlbumArtLoaded(object sender, ResultEventArgs args)
        {
            if (_isDisposed) return;

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
                        _artsCache.Clear();
                        _isLowMemory = true;
                        AmwLog.Warn(LogTag, "Out of Memory detected");
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

            _artsCache.Set(albumId, albumArt, false);
        }

        public Bitmap GetAlbumArt(long albumId)
        {
            Bitmap albumArt = null;

            if (_isLowMemory == false && _artsCache.TryGetValue(albumId, out albumArt) == false)
            {
                if (_isAlbumArtsLoaderEnabled && _requestedAlbumArts.Contains(albumId) == false)
                {
                    _loadArtRequestFactory.Submit(albumId, _artsCache);
                    _requestedAlbumArts.Add(albumId);
                }
            }

            return albumArt;
        }

        public void OnAlbumArtDisposed(object sender, EntryDisposedEventArgs args)
        {
            _requestedAlbumArts.Remove(args.Key);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            base.Dispose(disposing);

            if (disposing)
            {
                _requestListener.RemoveResultHandler(typeof(BatchRequest));
                _artsCache.Dispose();
                _requestedAlbumArts.Clear();
                _requestListener.Dispose();
                _albumArtsLoader.Dispose();
                _albumArtsLoader = null;
                _callbacks = null;
            }

            _isDisposed = true;
        }
    }
}