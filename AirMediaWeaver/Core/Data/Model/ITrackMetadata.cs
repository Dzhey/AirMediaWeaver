namespace AirMedia.Core.Data.Model
{
    public interface ITrackMetadata : ITrackSourceDefinition
    {
        string Artist { get; set; }
        string TrackTitle { get; set; }
        string Album { get; set; }
        int TrackDurationMillis { get; set; }
        string Genre { get; set; }
    }
}