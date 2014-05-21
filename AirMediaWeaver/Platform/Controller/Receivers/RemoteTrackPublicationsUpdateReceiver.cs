using System;
using AirMedia.Core.Log;
using Android.Content;

namespace AirMedia.Platform.Controller.Receivers
{
    public class RemoteTrackPublicationsUpdateReceiver : BroadcastReceiver
    {
        public const string ActionRemoteTrackPublicationsUpdated =
            "org.eb.airmedia.android.intent.notify.REMOTE_TRACK_PUBLICATIONS_UPDATED";

        public static readonly string LogTag = typeof (RemoteTrackPublicationsUpdateReceiver).Name;

        public event EventHandler<EventArgs> RemoteTrackPublicationsUpdate;

        public override void OnReceive(Context context, Intent intent)
        {
            switch (intent.Action)
            {
                case ActionRemoteTrackPublicationsUpdated:
                    AmwLog.Debug(LogTag, "broadcast received; remote track publications updated");
                    if (RemoteTrackPublicationsUpdate != null)
                    {
                        RemoteTrackPublicationsUpdate(this, EventArgs.Empty);
                    }
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format("unexpected broadcast intent received: \"{0}\"", intent));
                    break;
            }
        }
    }
}