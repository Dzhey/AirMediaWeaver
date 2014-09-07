using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Log;
using Android.Graphics;
using Java.Util.Concurrent.Atomic;

namespace AirMedia.Platform.Util
{
    public class AndroidBitmapPool
    {
        public static readonly string LogTag = typeof (AndroidBitmapPool).Name;

        public const int MaxSizeInBytes = 4*1024*1024;

        private readonly int _maxSizeInBytes;
        private readonly LinkedList<KeyValuePair<int, Bitmap>> _pool;
        private readonly AtomicInteger _sizeInBytes;
        private readonly Object _lock;

        public AndroidBitmapPool(int maxSizeInBytes = MaxSizeInBytes)
        {
            _lock = new Object();
            _sizeInBytes = new AtomicInteger();
            _pool = new LinkedList<KeyValuePair<int, Bitmap>>();
            _maxSizeInBytes = maxSizeInBytes;
        }

        private bool IsInBitmapMostAppropriate(Bitmap candidate, Bitmap inBitmap)
        {
            if (inBitmap == null) return false;
            if (candidate == null) return true;

            return BitmapUtils.GetBitmapSize(inBitmap) < BitmapUtils.GetBitmapSize(candidate);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
                _sizeInBytes.Set(0);
            }
        }

        public bool AddInBitmapOptions(BitmapFactory.Options options)
        {
            if (_pool.Count < 1)
            {
                AmwLog.Verbose(LogTag, "add InBitmapOptions: pool is empty");
                return false;
            }

            var inBitmapEntry = default(KeyValuePair<int, Bitmap>);
            lock (_lock)
            {
                foreach (var entry in _pool)
                {
                    if (BitmapUtils.CanUseForInBitmap(entry.Value, options))
                    {
                        if (IsInBitmapMostAppropriate(inBitmapEntry.Value, entry.Value))
                            inBitmapEntry = entry;
                    }
                }

                if (inBitmapEntry.Value != null)
                {
                    options.InMutable = true;
                    options.InBitmap = inBitmapEntry.Value;

                    RemoveEntry(inBitmapEntry);

                    AmwLog.Verbose(LogTag, string.Format(
                        "InBitmap added for {0}x{1} from {2}x{3}",
                        options.OutWidth, 
                        options.OutHeight, 
                        inBitmapEntry.Value.Width,
                        inBitmapEntry.Value.Height));

                    return true;
                }
            }


            AmwLog.Verbose(LogTag, string.Format(
                "InBitmap not found for {0}x{1}", options.OutWidth, options.OutHeight));

            return false;
        }

        public void Push(Bitmap bitmap)
        {
            if (bitmap == null) 
                throw new ArgumentException("can't put null value");

            if (bitmap.Handle == IntPtr.Zero)
            {
                AmwLog.Warn(LogTag, "attempt to put disposed bitmap object");
                return;
            }

            var entry = CreateEntry(bitmap);

            lock (_lock)
            {
                PerformCleanup(entry.Key);

                _pool.AddLast(entry);
                _sizeInBytes.AddAndGet(entry.Key);
            }
            AmwLog.Verbose(LogTag, string.Format("bitmap added; {0}x{1})", bitmap.Width, bitmap.Height));
        }

        public Bitmap Pop()
        {
            var bitmap = (Bitmap) null;

            if (_pool.Count < 1) return null;

            lock (_pool)
            {
                foreach (var entry in _pool)
                {
                    bitmap = entry.Value;
                    if (bitmap.Handle != IntPtr.Zero)
                        break;

                    AmwLog.Warn(LogTag, "disposed bitmap found");
                    DisposeEntry(entry);
                }
            }

            return bitmap;
        }

        private KeyValuePair<int, Bitmap> CreateEntry(Bitmap bitmap)
        {
            int sz = GetSizeOfValue(bitmap);

            return new KeyValuePair<int, Bitmap>(sz, bitmap);
        }

        private void PerformCleanup(int requiredSize)
        {
            var bytesToFree = (_sizeInBytes.Get() + requiredSize) - _maxSizeInBytes;

            if (bytesToFree <= 0) return;

            lock (_lock)
            {
                foreach (var entry in _pool.ToArray())
                {
                    bytesToFree = (_sizeInBytes.Get() + requiredSize) - _maxSizeInBytes;

                    if (bytesToFree <= 0) break;

                    DisposeEntry(entry);
                }
            }
        }

        private void RemoveEntry(KeyValuePair<int, Bitmap> entry)
        {
            lock (_lock)
            {
                if (!_pool.Remove(entry))
                    throw new ArgumentException("unable to remove entry from bitmap pool");

                _sizeInBytes.AddAndGet(-entry.Key);

                var bitmap = entry.Value;
                AmwLog.Verbose(LogTag, string.Format("bitmap removed; {0}x{1}", bitmap.Width, bitmap.Height));
            }
        }

        private void DisposeEntry(KeyValuePair<int, Bitmap> entry)
        {
            RemoveEntry(entry);
            var bitmap = entry.Value;
            if (bitmap.Handle != IntPtr.Zero)
            {
                bitmap.Recycle();
                bitmap.Dispose();
            }
        }

        private int GetSizeOfValue(Bitmap value)
        {
            return BitmapUtils.GetBitmapSize(value);
        }
    }
}