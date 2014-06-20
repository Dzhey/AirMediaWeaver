using System;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class AbsCachedRequest<T> : AbsRequest where T : RequestResult, ICachedRequestResult, new()
    {
        public const int CacheExpireDeltaMillisDefault = 10 * 60 * 1000;

        private readonly IRequestResultCache _cache;

        protected virtual bool DiscardOnExpire { get; set; }
        protected virtual int ExpireDeltaMillis { get; set; }

        protected AbsCachedRequest(IRequestResultCache cache)
        {
            _cache = cache;
        }

        protected sealed override RequestResult ExecuteImpl(out RequestStatus status)
        {
            UpdateEventArgs updateArgs;
            var key = GetCacheKey();

            if (key != null)
            {
                try
                {
                    var cachedResult = _cache.RetrieveCachedResult(key);
                    if (cachedResult != null)
                    {
                        updateArgs = CreateUpdateEventArgs(new CachedUpdateData {CachedResult = cachedResult});
                        NotifyRequestUpdate(updateArgs);
                    }
                }
                catch (Exception e)
                {
                    AmwLog.Warn(LogTag, "error retrieving cached result", e.ToString());
                }
            }

            var result = ExecuteCachedImpl(out status);
            updateArgs = CreateUpdateEventArgs(new IntermediateResultUpdateData {RequestResult = result});
            NotifyRequestUpdate(updateArgs);

            return result;
        }

        protected override void OnPostExecute(RequestResult result, RequestStatus status)
        {
            var typedResult = result as T;

            if (status != RequestStatus.Ok || result == null)
                return;

            var key = GetCacheKey();
            try
            {
                if (key != null && status == RequestStatus.Ok)
                {
                    int expire = ExpireDeltaMillis == 0 ? CacheExpireDeltaMillisDefault : ExpireDeltaMillis;

                    _cache.PerformCache(key, typedResult, expire, DiscardOnExpire);
                }
            }
            catch (Exception e)
            {
                AmwLog.Warn(LogTag, "Error trying to cache request result", e.ToString());
            }
        }

        protected abstract T ExecuteCachedImpl(out RequestStatus status);
        protected abstract string GetCacheKey();
    }
}