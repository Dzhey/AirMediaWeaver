using AirMedia.Core.Data;

namespace AirMedia.Core.Controller.PlaybackSource
{
    public interface IBasicPlaybackSource
    {
        bool HasNext();
        bool HasCurrent();
        bool HasPrevious();
        bool MoveNext();
        bool MovePrevious();
        ResourceDescriptor? GetCurrentResource();
    }
}