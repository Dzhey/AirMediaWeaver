using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Model;

namespace AirMedia.Core.Controller.WebService.Http
{
    public interface IHttpContentProvider : IAmwContentProvider
    {
        HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo();
        IHttpTrackContentDescriptor GetHttpTrackInfo(string trackGuid);
    }
}