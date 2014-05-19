

namespace AirMedia.Core.Data
{
    public interface ITrackSourceDefinition
    {
        string PeerGuid { get; set; }
        string TrackGuid { get; set; }
    }
}