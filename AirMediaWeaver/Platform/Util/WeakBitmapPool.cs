using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AirMedia.Core.Log;
using Android.Graphics;
using Java.Util.Concurrent.Atomic;

namespace AirMedia.Platform.Util
{
    public class WeakBitmapPool
    {
        public const int MaxSizeInBytes = 4*1024*1024;

        private readonly int _maxSizeInBytes;
        private readonly ConcurrentQueue<KeyValuePair<int, WeakReference<Bitmap>>> _pool;
        private readonly AtomicInteger _sizeInBytes;

        public WeakBitmapPool(int maxSizeInBytes = MaxSizeInBytes)
        {
            _sizeInBytes = new AtomicInteger();
            _pool = new ConcurrentQueue<KeyValuePair<int, WeakReference<Bitmap>>>();
            _maxSizeInBytes = maxSizeInBytes;
        }

        public void Put(Bitmap bitmap)
        {
            var entry = CreateEntry(bitmap);

            PerformCleanup(entry.Key);

            _pool.Enqueue(entry);
            _sizeInBytes.AddAndGet(entry.Key);
            AmwLog.Info("WeakBitmapPool", "item added; size, kb: " + _sizeInBytes.Get() / 1024);
        }

        public bool TryGetBitmap(out Bitmap bitmap)
        {
            bitmap = null;

            while (true)
            {
                KeyValuePair<int, WeakReference<Bitmap>> entry;
                if (!TryDequeue(out entry))
                {
                    return false;
                }

                if (entry.Value.TryGetTarget(out bitmap) && bitmap.Handle != IntPtr.Zero)
                {
                    break;
                }
                
                DisposeEntry(entry);
            }
            AmwLog.Info("WeakBitmapPool", "item retrieved from cache; size, kb: " + _sizeInBytes.Get() / 1024);

            return true;
        }

        private KeyValuePair<int, WeakReference<Bitmap>> CreateEntry(Bitmap bitmap)
        {
            int sz = GetSizeOfValue(bitmap);

            return new KeyValuePair<int, WeakReference<Bitmap>>(sz, new WeakReference<Bitmap>(bitmap));
        }

        private void PerformCleanup(int requiredSize)
        {
            var bytesToFree = (_sizeInBytes.Get() + requiredSize) - _maxSizeInBytes;

            if (bytesToFree <= 0) return;

            KeyValuePair<int, WeakReference<Bitmap>> entry;
            while (_pool.TryDequeue(out entry))
            {
                if (bytesToFree <= 0) break;

                DisposeEntry(entry);
            }
        }

        private bool TryDequeue(out KeyValuePair<int, WeakReference<Bitmap>> entry)
        {
            bool deque = _pool.TryDequeue(out entry);

            if (!deque) return false;
        
            int sz = entry.Key;
            _sizeInBytes.AddAndGet(-sz);

            // Reset entry used memory
            entry = new KeyValuePair<int, WeakReference<Bitmap>>(0, entry.Value);

            return true;
        }

        private void DisposeEntry(KeyValuePair<int, WeakReference<Bitmap>> entry)
        {
            int sz = entry.Key;
            var bmpRef = entry.Value;
            Bitmap bitmap;
            if (bmpRef.TryGetTarget(out bitmap))
            {
                if (bitmap.Handle != IntPtr.Zero)
                {
                    bitmap.Recycle();
                    bitmap.Dispose();
                }
            }

            if (sz != 0)
            {
                _sizeInBytes.AddAndGet(-sz);
                AmwLog.Info("WeakBitmapPool", "freed memory: " + sz + "; size, kb: " + _sizeInBytes.Get() / 1024);
            }
        }

        private int GetSizeOfValue(Bitmap value)
        {
            return BitmapUtils.GetBitmapSize(value);
        }
    }
}