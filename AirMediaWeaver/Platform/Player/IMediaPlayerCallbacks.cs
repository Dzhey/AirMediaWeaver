

using AirMedia.Core.Data.Model;

namespace AirMedia.Platform.Player
{
    public interface IMediaPlayerCallbacks : IPlaybackProgressListener
    {
        void OnPlaybackStarted();
        void OnPlaybackStopped();
        void OnTrackMetadataResolved(ITrackMetadata metadata);
    }
}