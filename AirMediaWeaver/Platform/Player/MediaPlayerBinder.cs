
using System.Collections.Generic;
using AirMedia.Platform.Data;
using Android.OS;

namespace AirMedia.Platform.Player
{
    public class MediaPlayerBinder : Binder
    {
        public MediaPlayerService Service { get; private set; }

        private readonly List<IMediaPlayerCallbacks> _listeners;

        public MediaPlayerBinder(MediaPlayerService service)
        {
            _listeners = new List<IMediaPlayerCallbacks>();
            Service = service;
        }

        public void AddMediaPlayerCallbacks(IMediaPlayerCallbacks callbacks)
        {
            lock (_listeners)
            {
                _listeners.Add(callbacks);
            }
        }

        public void RemoveMediaPlayerCallbacks(IMediaPlayerCallbacks callbacks)
        {
            lock (_listeners)
            {
                _listeners.Remove(callbacks);
            }
        }

        public void NotifyPlaybackStarted()
        {
            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    listener.OnPlaybackStarted();
                }
            }
        }

        public void NotifyPlaybackStopped()
        {
            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    listener.OnPlaybackStopped();
                }
            }
        }

        public void NotifyTrackMetadataResolved(TrackMetadata metadata)
        {
            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    listener.OnTrackMetadataResolved(metadata);
                }
            }
        }
    }
}