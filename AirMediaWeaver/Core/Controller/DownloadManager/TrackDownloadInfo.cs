

namespace AirMedia.Core.Controller.DownloadManager
{
    public struct TrackDownloadInfo
    {
        public int DownloadStatus { get; set; }
        public bool IsDownloaded { get; set; }
        public long BytesDownloadedSoFar { get; set; }
        public long TotalSizeBytes { get; set; }
    }
}