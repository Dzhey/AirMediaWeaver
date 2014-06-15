

namespace AirMedia.Core.Requests.Interfaces
{
    public interface IRequestResultCache
    {
        void PerformCache(string tag, ICachedRequestResult result, 
            int expireDeltaMillis, bool discardOnExpire);

        ICachedRequestResult RetrieveCachedResult(string tag);
    }
}