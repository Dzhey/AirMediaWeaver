using AirMedia.Core.Log;
using AirMedia.Platform.Player;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.UI.Player
{
    public class MediaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public static readonly string LogTag = typeof (MediaServiceConnection).Name;

        public MediaPlayerBinder Binder { get; private set; }
        public bool IsBound
        {
            get { return Binder != null; }
        }

        private IMediaPlayerCallbacks _callbacks;

        public MediaServiceConnection(IMediaPlayerCallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = (MediaPlayerBinder) service;
            Binder.AddMediaPlayerCallbacks(_callbacks);
        }

        public void Release()
        {
            if (IsBound && _callbacks != null)
            {
                Binder.RemoveMediaPlayerCallbacks(_callbacks);
                _callbacks = null;
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Binder = null;
            if (_callbacks != null)
            {
                AmwLog.Warn(LogTag, string.Format("media player service " +
                                                  "client leaked \"{0}\"", _callbacks));
            }
        }
    }
}