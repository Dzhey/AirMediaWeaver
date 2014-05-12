using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class BaseLoadRequest<T> : AbsRequest
    {
        protected sealed override RequestResult ExecuteImpl(out RequestStatus status)
        {
            return DoLoad(out status);
        }

        protected abstract LoadRequestResult<T> DoLoad(out RequestStatus status);
    }
}
