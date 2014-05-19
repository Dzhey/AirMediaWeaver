using AirMedia.Core.Data.Model;

namespace AirMedia.Platform.Data
{
    public struct HttpTrackContentDescriptor : IHttpTrackContentDescriptor
    {
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public long ContentLength { get; set; }

        public override string ToString()
        {
            return string.Format("[HttpTrackContentDescriptor(ContentType: \"{0}\"; FileName: \"{1}\"; " +
                                 "ContentLength: \"{2}\")]", ContentType, FilePath, ContentLength);
        }
    }
}