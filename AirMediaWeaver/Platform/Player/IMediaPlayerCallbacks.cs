

using AirMedia.Platform.Data;

namespace AirMedia.Platform.Player
{
    public interface IMediaPlayerCallbacks : IPlaybackProgressListener
    {
        void OnPlaybackStarted();
        void OnPlaybackStopped();
        void OnTrackMetadataResolved(TrackMetadata metadata);
    }
}