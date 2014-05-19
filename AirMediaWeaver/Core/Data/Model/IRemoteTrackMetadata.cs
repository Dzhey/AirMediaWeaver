

namespace AirMedia.Core.Data.Model
{
    public interface IRemoteTrackMetadata : ITrackMetadata
    {
        string ContentType { get; set; }
    }
}