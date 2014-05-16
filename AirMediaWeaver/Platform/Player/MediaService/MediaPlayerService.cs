using AirMedia.Core.Log;
using AirMedia.Platform.Controller.PlaybackSource;
using AirMedia.Platform.Controller.PlayerBackend;
using AirMedia.Platform.Data;
using Android.App;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Player.MediaService
{
    [Service(Exported = false, Enabled = true), 
    IntentFilter(new[] { ActionPlay, ActionStop })]
    public class MediaPlayerService : Service, IAmwPlayerCallbacks, IBinderCallbacks
    {
        public const string ExtraFastForward = "fast_forward";
        public const string ExtraPlaybackSource = "playback_source";
        public const string ActionEnqueue = "org.eb.airmedia.android.intent.action.ENQUEUE";
        public const string ActionPlay = "org.eb.airmedia.android.intent.action.PLAY";
        public const string ActionStop = "org.eb.airmedia.android.intent.action.STOP";
        public const string ActionPause = "org.eb.airmedia.android.intent.action.PAUSE";
        public const string ActionUnpause = "org.eb.airmedia.android.intent.action.UNPAUSE";
        public const string ActionFastForward = "org.eb.airmedia.android.intent.action.FAST_FORWARD";
        public const string ActionRewind = "org.eb.airmedia.android.intent.action.REWIND";

        private static readonly string LogTag = typeof (MediaPlayerService).Name;

        private AmwPlayer _player;
        private MediaPlayerBinder _binder;

        public override void OnCreate()
        {
            base.OnCreate();

            _binder = new MediaPlayerBinder(this);
            _player = new AmwPlayer();
            _player.Callbacks = this;

            AmwLog.Debug(LogTag, "MediaPlayerService created");
        }

        public override void OnDestroy()
        {
            _player.Release();
            _binder = null;

            base.OnDestroy();

            AmwLog.Debug(LogTag, "MediaPlayerService destroyed");
        }

        public override StartCommandResult OnStartCommand(Intent intent, 
            StartCommandFlags flags, int startId)
        {
            if (intent == null)
            {
                return StartCommandResult.Sticky;
            }

            switch (intent.Action)
            {
                case ActionEnqueue:
                    var parcel = (IParcelable) intent.GetParcelableExtra(ExtraPlaybackSource);
                    EnqueuePlaybackSource(parcel);
                    break;

                case ActionPlay:
                    bool fastForward = intent.GetBooleanExtra(ExtraFastForward, false);
                    if (fastForward)
                    {
                        _player.Stop();
                    }
                    _player.Play();
                    break;

                case ActionStop:
                    _player.Stop();
                    break;

                case ActionPause:
                    _player.Pause();
                    break;

                case ActionUnpause:
                    _player.Unpause();
                    break;

                case ActionFastForward:
                    _player.FastForward();
                    break;

                case ActionRewind:
                    _player.Rewind();
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format(
                        "Unhandled intent received \"{0}\"", intent.Action));
                    break;
            }

            return StartCommandResult.Sticky;
        }

        private void EnqueuePlaybackSource(IParcelable parcel)
        {
            var parcelledSource = parcel as LocalLibraryPlaybackSourceParcel;
            if (parcelledSource == null)
            {
                AmwLog.Error(LogTag, string.Format("can't enqueue \"{0}\"", parcel.GetType()));

                return;
            }

            var source = LocalLibraryPlaybackSource.CreateFromParcel(parcelledSource);

            _player.ResetQueue();
            _player.Enqueue(source);

            AmwLog.Debug(LogTag, string.Format("enqueud \"{0}\"", source));
        }

        public override IBinder OnBind(Intent intent)
        {
            return _binder;
        }

        public void OnPlaybackCompleted()
        {
            StopSelf();
        }

        public void OnSeekComplete()
        {
            if (_binder != null)
            {
                _binder.NotifySeekCompleted();
            }
        }

        public void OnPlaybackStopped()
        {
            if (_binder != null)
            {
                _binder.NotifyPlaybackStopped();
            }
        }

        public void OnPlaybackStarted()
        {
            if (_binder != null)
            {
                _binder.NotifyPlaybackStarted();
            }
        }

        public void OnTrackMetadataResolved(TrackMetadata metadata)
        {
            if (_binder != null)
            {
                _binder.NotifyTrackMetadataResolved(metadata);
            }
        }

        public bool SeekTo(int locationMillis)
        {
            return _player.SeekTo(locationMillis);
        }

        public int GetDuration()
        {
            return _player.GetDuration();
        }

        public int GetCurrentPosition()
        {
            return _player.GetCurrentPosition();
        }

        public bool IsPaused()
        {
            return _player.Status == PlaybackStatus.Paused;
        }

        public bool IsPlaying()
        {
            return _player.IsPlaying();
        }

        public bool Pause()
        {
            return _player.Pause();
        }

        public bool Unpause()
        {
            return _player.Unpause();
        }

        public TrackMetadata? GetTrackMetadata()
        {
            return _player.GetTrackMetadata();
        }
    }
}