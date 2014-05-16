using System.Collections.Generic;
using AirMedia.Core.Log;
using AirMedia.Platform.Data;
using Android.OS;

namespace AirMedia.Platform.Player.MediaService
{
    public class MediaPlayerBinder : Binder
    {
        private const int PlaybackProgressUpdateIntervalMillis = 200;
        private static readonly string LogTag = typeof (MediaPlayerBinder).Name;

        private readonly List<IMediaPlayerCallbacks> _listeners;
        private readonly List<IPlaybackProgressListener> _playbackProgressListeners;

        private readonly IBinderCallbacks _callbacks;
        private bool _isProgressUpdating;
        private bool _isSeeking;

        public MediaPlayerBinder(IBinderCallbacks callbacks)
        {
            _listeners = new List<IMediaPlayerCallbacks>();
            _playbackProgressListeners = new List<IPlaybackProgressListener>();
            _callbacks = callbacks;
        }

        public TrackMetadata? GetTrackMetadata()
        {
            return _callbacks.GetTrackMetadata();
        }

        public bool IsPlaying()
        {
            return _callbacks.IsPlaying();
        }

        public bool IsPaused()
        {
            return _callbacks.IsPaused();
        }

        public bool TogglePause()
        {
            if (_callbacks.IsPaused())
            {
                return _callbacks.Unpause();
            }
            
            return _callbacks.Pause();
        }

        public void SeekTo(int location)
        {
            _isSeeking = true;
            if (_callbacks.SeekTo(location) == false)
            {
                _isSeeking = false;
                AmwLog.Error(LogTag, "one tried to seek from wrong player state");
            }
        }

        public void NotifySeekCompleted()
        {
            _isSeeking = false;
        }

        public void AddPlaybackProgressListener(IPlaybackProgressListener listener)
        {
            if (_playbackProgressListeners.Count == 0)
            {
                BeginPlaybackProgressUpdates();
            }

            lock (_playbackProgressListeners)
            {
                _playbackProgressListeners.Add(listener);
            }
        }

        public void RemovePlaybackProgressListener(IPlaybackProgressListener listener)
        {
            lock (_playbackProgressListeners)
            {
                _playbackProgressListeners.Remove(listener);
            }
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

            if (_playbackProgressListeners.Count > 0)
            {
                BeginPlaybackProgressUpdates();
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

            StopPlaybackProgressUpdates();
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

        private void NotifyPlaybackProgressUpdate()
        {
            if (_isSeeking == false)
            {
                int duration = _callbacks.GetDuration();
                int position = _callbacks.GetCurrentPosition();
                lock (_playbackProgressListeners)
                {
                    foreach (var listener in _playbackProgressListeners)
                    {
                        listener.OnPlaybackProgressUpdate(position, duration);
                    }
                }
            }

            if (_isProgressUpdating)
            {
                App.MainHandler.PostDelayed(NotifyPlaybackProgressUpdate,
                    PlaybackProgressUpdateIntervalMillis);
            }
        }

        private void BeginPlaybackProgressUpdates()
        {
            _isProgressUpdating = true;
            App.MainHandler.PostDelayed(NotifyPlaybackProgressUpdate,
                PlaybackProgressUpdateIntervalMillis);
        }

        private void StopPlaybackProgressUpdates()
        {
            _isProgressUpdating = false;
        }
    }
}