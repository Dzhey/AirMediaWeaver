using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests.Interfaces
{
    public interface IContextualWorkerCallbacks
    {
        void OnWorkerRequestError(int errorCode = 0, string errorMessage = null);
        void OnWorkerRequestFinished(ResultEventArgs args);
        void OnWorkerRequestUpdate(UpdateEventArgs args);
    }
}