using System;
using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Core.Utils;
using Android.Graphics;
using Android.OS;

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
        public const int MaxReuseBitmapCacheSizeDefault = 6*1024*1024;

        public event EventHandler<EntryDisposedEventArgs> EntryDisposed;

        private readonly LruCache<TKey, Bitmap> _cache;
        private bool _isDisposed;
        private readonly WeakBitmapPool _reuseBitmapsPool;


        public LruReuseReuseBitmapCache(int maxBitmapCacheSize = MaxBitmapCacheSizeBytesDefault)
        {
            _cache = new LruCache<TKey, Bitmap>(maxBitmapCacheSize, this);
            _reuseBitmapsPool = new WeakBitmapPool(MaxReuseBitmapCacheSizeDefault);
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
                _reuseBitmapsPool.Put(value);
                return;
            }
            
            _cache.Set(key, value);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void AddInBitmapOptions(BitmapFactory.Options options)
        {
            var putBackList = new List<Bitmap>();
            Bitmap bmp;
            while (_reuseBitmapsPool.TryGetBitmap(out bmp))
            {
                if (CanUseForInBitmap(bmp, options))
                {
                    options.InMutable = true;
                    options.InBitmap = bmp;

                    break;
                }
                
                putBackList.Add(bmp);
            }

            // Put unused bitmaps back to pool
            foreach (var bitmap in putBackList)
            {
                _reuseBitmapsPool.Put(bitmap);
            }
        }

        public void AddReusableBitmap(Bitmap bitmap)
        {
            _reuseBitmapsPool.Put(bitmap);
        }

        public int GetSizeOfValue(TKey key, Bitmap value)
        {
            if (value == null || value.Handle == IntPtr.Zero) return 0;

            return GetBytesPerPixel(value.GetConfig()) * value.Width * value.Height;
        }

        public void DisposeOfValue(TKey key, Bitmap value)
        {
            if (value != null)
            {
                _reuseBitmapsPool.Put(value);
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

        private bool CanUseForInBitmap(Bitmap candidate, BitmapFactory.Options targetOptions)
        {
            if (candidate.Handle == IntPtr.Zero)
            {
                AmwLog.Warn(LogTag, "tried to use unbound bitmap");

                return false;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                int width = targetOptions.OutWidth / targetOptions.InSampleSize;
                int height = targetOptions.OutHeight / targetOptions.InSampleSize;
                int byteCount = width * height * GetBytesPerPixel(candidate.GetConfig());

                return byteCount <= candidate.AllocationByteCount;
            }

            return candidate.Width == targetOptions.OutWidth
                    && candidate.Height == targetOptions.OutHeight
                    && targetOptions.InSampleSize == 1;
        }

        private static int GetBytesPerPixel(Bitmap.Config config)
        {
            if (config == Bitmap.Config.Argb8888)
            {
                return 4;
            }

            if (config == Bitmap.Config.Rgb565)
            {
                return 2;
            }

            if (config == Bitmap.Config.Argb4444)
            {
                return 2;
            }

            if (config == Bitmap.Config.Alpha8)
            {
                return 1;
            }

            return 1;
        }
    }
}