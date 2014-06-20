using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class BaseLoadCachedRequest<T> : AbsCachedRequest<CachedLoadRequestResult<T>>
    {
        protected BaseLoadCachedRequest(IRequestResultCache cache) : base(cache)
        {
        }

        protected sealed override CachedLoadRequestResult<T> ExecuteCachedImpl(out RequestStatus status)
        {
            return DoLoad(out status);
        }

        protected abstract CachedLoadRequestResult<T> DoLoad(out RequestStatus status);
    }
}