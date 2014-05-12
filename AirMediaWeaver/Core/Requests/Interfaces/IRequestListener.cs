using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Interfaces
{
    public interface IRequestListener
    {
    }

    public interface IRequestResultListener : IRequestListener
    {
        void HandleRequestResult(object sender, ResultEventArgs args);
    }

    public interface IRequestUpdateListener : IRequestListener
    {
        void HandleRequestUpdate(object sender, UpdateEventArgs args);
    }
}
