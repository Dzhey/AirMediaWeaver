

namespace AirMedia.Core.Controller.DownloadManager
{
    public interface ITrackDownloadsManager
    {
        bool IsTrackDownloadPresented(string trackGuid);
        bool IsTrackDownloaded(string trackGuid);
        int GetDownloadStatus(string trackGuid);
        void EnqueueDownload(string trackGuid);
    }
}