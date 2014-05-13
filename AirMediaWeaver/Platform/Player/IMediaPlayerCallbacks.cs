

using AirMedia.Platform.Data;

namespace AirMedia.Platform.Player
{
    public interface IMediaPlayerCallbacks
    {
        void OnPlaybackStarted();
        void OnPlaybackStopped();
        void OnTrackMetadataResolved(TrackMetadata metadata);
    }
}