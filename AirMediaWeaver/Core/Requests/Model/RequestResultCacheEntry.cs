using System;


namespace AirMedia.Core.Requests.Model
{
    public class RequestResultCacheEntry
    {
        public string ModelTypeName { get; set; }
        public DateTime CacheTimestamp { get; set; }
        public byte[] Data { get; set; }
        public int ExpireDeltaMillis { get; set; }
        public bool DiscardOnExpire { get; set; }
        public DateTime LastAccess { get; set; }
    }
}