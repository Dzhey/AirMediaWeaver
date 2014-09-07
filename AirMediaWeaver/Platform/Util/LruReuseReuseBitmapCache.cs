using System;
using AirMedia.Core.Utils;
using Android.Graphics;

namespace AirMedia.Platform.Util
{
    public class LruReuseReuseBitmapCache<TKey> : IReuseBitmapCacheAccessor, LruCache<TKey, Bitmap>.ICacheEntryHandler, IDisposable
    {
        public class EntryDisposedEventArgs : EventArgs
        {
            public TKey Key { get; private set; }

            public EntryDisposedEventArgs(TKey key)
            {
                Key = key;
            }
        }

        public static readonly string LogTag = typeof (LruReuseReuseBitmapCache<TKey>).Name;

        public const int MaxBitmapCacheSizeBytesDefault = 8*1024*1024;
        public const int MaxReuseBitmapCacheSizeDefault = 4*1024*1024;

        public event EventHandler<EntryDisposedEventArgs> EntryDisposed;

        private readonly LruCache<TKey, Bitmap> _cache;
        private bool _isDisposed;
        private readonly AndroidBitmapPool _reuseBitmapsPool;


        public LruReuseReuseBitmapCache(int maxBitmapCacheSize = MaxBitmapCacheSizeBytesDefault)
        {
            _cache = new LruCache<TKey, Bitmap>(maxBitmapCacheSize, this);
            _reuseBitmapsPool = new AndroidBitmapPool(MaxReuseBitmapCacheSizeDefault);
        }

        public bool TryGetValue(TKey key, out Bitmap value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void DisposeValue(TKey key)
        {
            _cache.DisposeNode(key);
        }

        public void Set(TKey key, Bitmap value, bool replaceValue = true)
        {
            // Move bitmap value to reuse cache if it won't replace current value
            Bitmap existingValue;
            if (replaceValue == false && _cache.TryGetValue(key, out existingValue))
            {
                _reuseBitmapsPool.Push(value);
                return;
            }
            
            _cache.Set(key, value);
        }

        public void Clear()
        {
            _cache.Clear();
            _reuseBitmapsPool.Clear();
        }

        public bool AddInBitmapOptions(BitmapFactory.Options options)
        {
            return _reuseBitmapsPool.AddInBitmapOptions(options);
        }

        public void AddReusableBitmap(Bitmap bitmap)
        {
            if (bitmap != null)
                _reuseBitmapsPool.Push(bitmap);
        }

        public int GetSizeOfValue(TKey key, Bitmap value)
        {
            if (value == null || value.Handle == IntPtr.Zero) return 0;

            return BitmapUtils.GetBytesPerPixel(value.GetConfig()) * value.Width * value.Height;
        }

        public void DisposeOfValue(TKey key, Bitmap value)
        {
            if (value != null)
            {
                _reuseBitmapsPool.Push(value);
            }

            if (EntryDisposed != null)
            {
                EntryDisposed(this, new EntryDisposedEventArgs(key));
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
                EntryDisposed = null;
                _cache.Clear();
            }

            _isDisposed = true;
        }
    }
}