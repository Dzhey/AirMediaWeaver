using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Data;

namespace AirMedia.Platform.Controller.WebService
{
    public interface IAmwStreamerService
    {
        PeerManager GetPeerManager();
    }
}