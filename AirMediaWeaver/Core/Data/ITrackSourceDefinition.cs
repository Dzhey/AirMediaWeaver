

namespace AirMedia.Core.Data
{
    public interface ITrackSourceDefinition
    {
        long TrackId { get; set; }
        string PeerGuid { get; set; }
        string TrackGuid { get; set; }
    }
}