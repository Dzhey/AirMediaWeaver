

namespace AirMedia.Core.Controller.WebService
{
    public interface IMulticastReceiver
    {
        bool IsStarted { get; }
        int Receive(byte[] buffer);
    }
}