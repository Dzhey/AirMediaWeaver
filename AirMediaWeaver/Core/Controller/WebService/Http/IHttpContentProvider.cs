using AirMedia.Core.Controller.WebService.Model;

namespace AirMedia.Core.Controller.WebService.Http
{
    public interface IHttpContentProvider
    {
        HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo();
    }
}