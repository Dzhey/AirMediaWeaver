using AirMedia.Core.Data.Model;

namespace AirMedia.Core.Data
{
    public interface ITrackPublicationsDao
    {
        ITrackMetadata QueryForGuid(string trackGuid);
        string[] GetPublishedTrackGuids();
    }
}