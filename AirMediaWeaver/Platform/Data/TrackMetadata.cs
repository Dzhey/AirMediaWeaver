using AirMedia.Core.Data.Model;

namespace AirMedia.Platform.Data
{
    public struct TrackMetadata : ITrackMetadata
    {
        public string PeerGuid { get; set; }
        public string TrackGuid { get; set; }
        public long TrackId { get; set; }
        public string Artist { get; set; }
        public string TrackTitle { get; set; }
        public string Album { get; set; }
        public int TrackDurationMillis { get; set; }
        public string Data { get; set; }
    }
}