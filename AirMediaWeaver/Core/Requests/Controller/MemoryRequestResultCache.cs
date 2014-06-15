using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Controller
{
    public class MemoryRequestResultCache : IRequestResultCache
    {
        private static readonly string LogTag = typeof (MemoryRequestResultCache).Name;

        public const int MaxSizeInBytesDefault = 256 * 1024;
        public const int CacheEntryExpireDeltaMillisDefault = 10 * 60 * 1000;

        private readonly IDictionary<string, RequestResultCacheEntry> _cache;

        private readonly int _maxSizeInBytes;
        private int _currentSizeInBytes;

        public MemoryRequestResultCache(int maxSizeInBytes = MaxSizeInBytesDefault)
        {
            if (maxSizeInBytes < 1) 
                throw new ArgumentOutOfRangeException("invalid max size");

            _cache = new Dictionary<string, RequestResultCacheEntry>();
            _maxSizeInBytes = maxSizeInBytes;
        }

        public void PerformCache(string tag, ICachedRequestResult result, 
            int expireDeltaMillis = CacheEntryExpireDeltaMillisDefault, bool discardOnExpire = true)
        {
            if (expireDeltaMillis < 1)
            {
                throw new ArgumentOutOfRangeException("expire delta is out of expected range");
            }

            var data = result.Serialize();
            var typeName = result.GetType().FullName;
            var ts = DateTime.UtcNow;

            lock (_cache)
            {
                DiscardCacheEntry(tag);
                InsertCacheEntry(tag, new RequestResultCacheEntry
                {
                    ModelTypeName = typeName,
                    CacheTimestamp = ts,
                    Data = data,
                    DiscardOnExpire = discardOnExpire,
                    ExpireDeltaMillis = expireDeltaMillis,
                    LastAccess = DateTime.MinValue
                });

                PerformCleanup();
            }
        }

        public ICachedRequestResult RetrieveCachedResult(string tag)
        {
            RequestResultCacheEntry entry;
            lock (_cache)
            {
                DiscardExpiredEntries();

                if (_cache.ContainsKey(tag) == false) return null;

                entry = _cache[tag];
            }

            var type = Type.GetType(entry.ModelTypeName);

            if (type == null)
            {
                AmwLog.Error(LogTag, entry.ModelTypeName, "unable to retrieve cache entry model type");
                return null;
            }

            var resultType = (ICachedRequestResult) Activator.CreateInstance(type);

            resultType.Deserialize(entry.Data);

            entry.LastAccess = DateTime.UtcNow;

            return resultType;
        }

        private void PerformCleanup()
        {
            lock (_cache)
            {
                DiscardExpiredEntries();

                while (_currentSizeInBytes > _maxSizeInBytes && _cache.Count > 0)
                {
                    var item = _cache.Aggregate((l, r) => (l.Value.LastAccess < r.Value.LastAccess ? l : r));
                    DiscardCacheEntry(item.Key);
                }
            }
        }

        private void DiscardExpiredEntries()
        {
            var now = DateTime.UtcNow;

            lock (_cache)
            {
                var items = _cache.Where(item => item.Value.DiscardOnExpire);

                foreach (var item in items)
                {
                    if (IsExpired(now, item.Value))
                    {
                        DiscardCacheEntry(item.Key);
                    }
                }
            }
        }

        protected virtual void InsertCacheEntry(string tag, RequestResultCacheEntry entry)
        {
            _cache.Add(tag, entry);
            _currentSizeInBytes += entry.Data.Length;
        }

        protected virtual void DiscardCacheEntry(string tag)
        {
            if (_cache.ContainsKey(tag))
            {
                var item = _cache[tag];
                _cache.Remove(tag);
                _currentSizeInBytes -= item.Data.Length;
            }
        }

        protected virtual bool IsExpired(DateTime now, RequestResultCacheEntry entry)
        {
            if (now.Kind != DateTimeKind.Utc) 
                throw new ArgumentException("now is not in utc base");

            var t1 = entry.CacheTimestamp.AddMilliseconds(entry.ExpireDeltaMillis).Ticks;

            return (t1 - now.Ticks) <= 0;
        }
    }
}