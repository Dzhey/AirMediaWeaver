

using AirMedia.Platform.Data;

namespace AirMedia.Platform.Controller.PlayerBackend
{
    public interface IAmwPlayerCallbacks
    {
        void OnSeekComplete();
        void OnPlaybackCompleted();
        void OnPlaybackStopped();
        void OnPlaybackStarted();
        void OnTrackMetadataResolved(TrackMetadata metadata);
    }
}