using AirMedia.Core.Log;
using AirMedia.Platform.Player;
using AirMedia.Platform.Player.MediaService;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.UI.Player
{
    public class MediaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public interface IConnectionListener
        {
            void OnServiceConnected(MediaPlayerBinder binder);
            void OnServiceDisconnected();
        }

        public static readonly string LogTag = typeof (MediaServiceConnection).Name;

        public MediaPlayerBinder Binder { get; private set; }
        public bool IsBound
        {
            get { return Binder != null; }
        }

        private IConnectionListener _connectionListener;
        private IMediaPlayerCallbacks _callbacks;

        public MediaServiceConnection(IConnectionListener connectionListener, 
            IMediaPlayerCallbacks callbacks)
        {
            _callbacks = callbacks;
            _connectionListener = connectionListener;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = (MediaPlayerBinder) service;
            Binder.AddMediaPlayerCallbacks(_callbacks);
            Binder.AddPlaybackProgressListener(_callbacks);
            if (_connectionListener != null)
            {
                _connectionListener.OnServiceConnected(Binder);
            }
        }

        public void Release()
        {
            if (IsBound && _callbacks != null)
            {
                Binder.RemoveMediaPlayerCallbacks(_callbacks);
                Binder.RemovePlaybackProgressListener(_callbacks);
                _callbacks = null;
                _connectionListener = null;
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Binder = null;
            if (_connectionListener != null)
            {
                _connectionListener.OnServiceDisconnected();
            }
            if (_callbacks != null)
            {
                AmwLog.Warn(LogTag, string.Format("media player service " +
                                                  "client leaked \"{0}\"", _callbacks));
            }
        }
    }
}