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
        public string ContentType { get; set; }
        public string Genre { get; set; }

        public override string ToString()
        {
            return string.Format("[TrackMetadata(" +
                                 "TrackId: \"{0}\" , " +
                                 "PeerGuid: \"{1}\", " +
                                 "TrackGuid: \"{2}\", " +
                                 "Artist: \"{3}\", " +
                                 "TrackTitle: \"{4}\", " +
                                 "Album: \"{5}\", " +
                                 "TrackDuratonMillis: \"{6}\", " +
                                 "ContentType: \"{7}\"," +
                                 "Genre: \"{8}\")]",
                                 TrackId,
                                 PeerGuid,
                                 TrackGuid,
                                 Artist,
                                 TrackTitle,
                                 Album,
                                 TrackDurationMillis,
                                 ContentType,
                                 Genre);
        }
    }
}