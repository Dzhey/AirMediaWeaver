using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Factory;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Controller;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Logger;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using AirMedia.Platform.Util;
using Android.Graphics;

namespace AirMedia.Platform.UI.Library.AlbumList.Controller
{
    public class AlbumsCoverProvider : IAlbumsCoverProvider
    {
        public static readonly string LogTag = typeof (AlbumsCoverProvider).Name;

        public const int MaxBitmapCacheSizeBytes = 1024 * 1024 * 4;

        private const string BatchLoadArtsRequestTag = "load_album_arts_batch_request";
        private const int ArtsLoaderThreadPoolSize = 2;
        private const int AlbumArtsLoaderBatchPeriodMillis = 100;

        public event EventHandler<AlbumArtLoadedEventArgs> AlbumCoverLoaded;
        public event EventHandler<AlbumArtsLoaderEnabledEventArgs> AlbumArtsLoaderEnabled;

        public bool IsAlbumArtsLoaderEnabled
        {
            get { return _isAlbumArtsLoaderEnabled; }
            set
            {
                _isAlbumArtsLoaderEnabled = value;
                if (AlbumArtsLoaderEnabled != null)
                {
                    AlbumArtsLoaderEnabled(this, new AlbumArtsLoaderEnabledEventArgs
                    {
                        IsAlbumArtsEnabled = _isAlbumArtsLoaderEnabled
                    });
                }
            }
        }

        private RequestManager _albumArtsLoader;
        private readonly IRequestFactory _loadArtRequestFactory;
        private readonly RequestResultListener _requestListener;
        private readonly ISet<long> _requestedAlbumArts;
        private readonly LruReuseReuseBitmapCache<long> _artsCache;
        private bool _isDisposed;
        private bool _isLowMemory;
        private bool _isAlbumArtsLoaderEnabled = true;

        public AlbumsCoverProvider()
        {
            _requestedAlbumArts = new HashSet<long>();
            _albumArtsLoader = new ThreadPoolRequestManager(new AndroidThreadPoolWorker(ArtsLoaderThreadPoolSize));
            _requestListener = RequestResultListener.NewInstance(LogTag, _albumArtsLoader);

            var factory = BatchRequestFactory.Init(typeof(LoadAlbumArtRequest), typeof(LoadAlbumArtBatchRequest));
            factory.FlushTimeoutMillis = AlbumArtsLoaderBatchPeriodMillis;

            _loadArtRequestFactory = AndroidRequestFactory.Init(factory, _requestListener)
                                                          .SetManager(_albumArtsLoader)
                                                          .SetParallel(true)
                                                          .SetActionTag(BatchLoadArtsRequestTag);

            _requestListener.RegisterResultHandler(typeof(BatchRequest), OnAlbumArtLoaded);
            _artsCache = new LruReuseReuseBitmapCache<long>(MaxBitmapCacheSizeBytes);
            _artsCache.EntryDisposed += OnAlbumArtDisposed;
        }
        public void AddAlbumArts(IEnumerable<KeyValuePair<long, Bitmap>> albumArts)
        {
            if (_isDisposed) return;

            foreach (var entry in albumArts)
            {
                _artsCache.Set(entry.Key, entry.Value, false);
            }
        }

        public Bitmap RequestAlbumCover(long albumId)
        {
            Bitmap albumArt = null;

            if (_isLowMemory == false && _artsCache.TryGetValue(albumId, out albumArt) == false)
            {
                if (IsAlbumArtsLoaderEnabled && _requestedAlbumArts.Contains(albumId) == false)
                {
                    _loadArtRequestFactory.Submit(albumId, _artsCache);
                    _requestedAlbumArts.Add(albumId);
                }
            }

            return albumArt;
        }

        public void ResetCache()
        {
            _artsCache.Clear();
            _requestedAlbumArts.Clear();
        }

        public void OnAlbumArtDisposed(object sender, LruReuseReuseBitmapCache<long>.EntryDisposedEventArgs args)
        {
            _requestedAlbumArts.Remove(args.Key);
            AmwLog.Verbose(LogTag, "album art removed from cache: " + args.Key);
        }

        private void OnAlbumArtLoaded(object sender, ResultEventArgs args)
        {
            if (_isDisposed) return;

            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, "Error loading album arts");
                return;
            }

            var results = ((BatchRequestResult)args.Result).Results;

            foreach (var result in results)
            {
                var rq = (LoadAlbumArtRequest)result.Key;

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
            _artsCache.Set(albumId, albumArt, false);
            AmwLog.Verbose(LogTag, "album art loaded: " + albumId);

            if (AlbumCoverLoaded != null)
            {
                AlbumCoverLoaded(this, new AlbumArtLoadedEventArgs
                {
                    AlbumId = albumId,
                    AlbumArt = albumArt
                });
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _requestListener.RemoveResultHandler(typeof(BatchRequest));
                _artsCache.Dispose();
                _requestedAlbumArts.Clear();
                _requestListener.Dispose();
                _albumArtsLoader.Dispose();
                _albumArtsLoader = null;
            }

            _isDisposed = true;
        }
    }
}