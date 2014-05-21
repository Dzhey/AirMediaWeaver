namespace AirMedia.Core.Data.Model
{
    public interface ITrackMetadata : ITrackSourceDefinition
    {
        long TrackId { get; set; }
        string Artist { get; set; }
        string TrackTitle { get; set; }
        string Album { get; set; }
        int TrackDurationMillis { get; set; }
        string Genre { get; set; }
    }
}