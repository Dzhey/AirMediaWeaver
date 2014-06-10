

namespace AirMedia.Core.Controller.WebService
{
    public interface IMulticastSender
    {
        bool IsStarted { get; }
        bool IsClientInitialized { get; }
        int SendMulticast(byte[] data, int offset, int length);
    }
}