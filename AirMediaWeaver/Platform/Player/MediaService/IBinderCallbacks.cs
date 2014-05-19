using AirMedia.Core.Data.Model;

namespace AirMedia.Platform.Player.MediaService
{
    public interface IBinderCallbacks
    {
        bool SeekTo(int locationMillis);
        int GetDuration();
        int GetCurrentPosition();
        bool IsPaused();
        bool IsPlaying();
        bool Pause();
        bool Unpause();
        ITrackMetadata GetTrackMetadata();
    }
}