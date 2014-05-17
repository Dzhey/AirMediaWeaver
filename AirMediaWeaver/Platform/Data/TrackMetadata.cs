
namespace AirMedia.Platform.Data
{
    public struct TrackMetadata
    {
        public string TrackGuid { get; set; }
        public long TrackId { get; set; }
        public string ArtistName { get; set; }
        public string TrackTitle { get; set; }
        public string Album { get; set; }
        public int Duration { get; set; }
        public string Data { get; set; }
    }
}