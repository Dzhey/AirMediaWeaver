

namespace AirMedia.Platform.Player
{
    public interface IPlaybackProgressListener
    {
        void OnPlaybackProgressUpdate(int current, int duration);
    }
}