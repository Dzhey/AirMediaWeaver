
using AirMedia.Core.Log;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.OS;

namespace AirMedia.Platform.Player
{
    [Service(Exported = false, Enabled = true), 
    IntentFilter(new[] { ActionPlay, ActionStop })]
    public class MediaPlayerService : Service, 
        MediaPlayer.IOnPreparedListener, 
        MediaPlayer.IOnErrorListener, 
        MediaPlayer.IOnCompletionListener
    {
        public const string ActionPlay = "org.eb.airmedia.android.intent.action.PLAY";
        public const string ActionStop = "org.eb.airmedia.android.intent.action.STOP";

        private static readonly string LogTag = typeof (MediaPlayerService).Name;

        private MediaPlayer _player;

        public override void OnCreate()
        {
            base.OnCreate();

            AmwLog.Debug(LogTag, "MediaPlayerService created");
        }

        public override void OnDestroy()
        {
            ReleasePlayer();

            base.OnDestroy();

            AmwLog.Debug(LogTag, "MediaPlayerService destroyed");
        }

        public override StartCommandResult OnStartCommand(Intent intent, 
            StartCommandFlags flags, int startId)
        {
            switch (intent.Action)
            {
                case ActionPlay:
                    StartPlayback(intent.Data);
                    break;

                case ActionStop:
                    ReleasePlayer();
                    StopSelf();
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format(
                        "Unhandled intent received \"{0}\"", intent.Action));
                    break;
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        private void StartPlayback(Uri uri)
        {
            InitPlayer();

            if (uri == null)
            {
                AmwLog.Error(LogTag, "Uri to prepare playback is not specified");
            }

            AmwLog.Debug(LogTag, string.Format("preparing \"{0}\" to play..", uri));
            _player.SetDataSource(this, uri);
            _player.PrepareAsync();
        }

        public void OnPrepared(MediaPlayer mp)
        {
            AmwLog.Debug(LogTag, "MediaPlayer prepared, performing playback..");
            mp.Start();
        }

        public bool OnError(MediaPlayer mp, MediaError what, int extra)
        {
            AmwLog.Error(LogTag, string.Format("received MediaPlayer error:" +
                                               " \"{0}\"; extra: \"{1}\"", what, extra));
            ReleasePlayer();

            return true;
        }

        public void OnCompletion(MediaPlayer mp)
        {
            AmwLog.Info(LogTag, "playback completed");
            ReleasePlayer();
            StopSelf();
        }

        private void InitPlayer()
        {
            if (_player == null)
            {
                _player = new MediaPlayer();
            }
            else
            {
                _player.Reset();
            }

            _player.SetAudioStreamType(Stream.Music);
            _player.SetOnCompletionListener(this);
            _player.SetOnErrorListener(this);
            _player.SetOnPreparedListener(this);
        }

        private void ReleasePlayer()
        {
            if (_player == null) return;

            _player.Reset();
            _player = null;
        }
    }
}