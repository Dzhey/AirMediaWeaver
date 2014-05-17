

namespace AirMedia.Core.Controller.WebService.Model
{
    public struct HttpBaseTrackInfo
    {
        public string PublicGuid { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Title { get; set; }
        public int DurationMillis { get; set; }
    }
}