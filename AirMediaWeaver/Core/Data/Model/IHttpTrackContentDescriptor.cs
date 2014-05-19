namespace AirMedia.Core.Data.Model
{
    public interface IHttpTrackContentDescriptor
    {
        string ContentType { get; set; }
        string FilePath { get; set; }
        string FileName { get; set; }
        long ContentLength { get; set; }
    }
}