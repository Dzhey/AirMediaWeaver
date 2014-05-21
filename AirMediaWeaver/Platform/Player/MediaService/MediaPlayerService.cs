using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Dao;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.PlaybackSource;
using AirMedia.Platform.Controller.PlayerBackend;
using AirMedia.Platform.Controller.WebService.Http;
using AirMedia.Platform.Data.Dao;
using AirMedia.Platform.Data.Sql.Dao;
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
        private PlayCountDao _playCountDao;

        public override void OnCreate()
        {
            base.OnCreate();

            _binder = new MediaPlayerBinder(this);
            _player = new AmwPlayer();
            _playCountDao = new AndroidPlayCountDao();
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
                    AmwLog.Debug(LogTag, "action play");
                    bool fastForward = intent.GetBooleanExtra(ExtraFastForward, false);
                    if (fastForward)
                    {
                        _player.Stop();
                    }
                    _player.Play();
                    AmwLog.Debug(LogTag, "after action play");
                    break;

                case ActionStop:
                    AmwLog.Debug(LogTag, "action stop");
                    _player.Stop();
                    AmwLog.Debug(LogTag, "after action stop");
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
            IBasicPlaybackSource source = null;
            if (parcel is LocalLibraryPlaybackSourceParcel)
            {
                source = LocalLibraryPlaybackSource.CreateFromParcel(
                    (LocalLibraryPlaybackSourceParcel) parcel);

            } 
            else if (parcel is RemotePlaybackSourceParcel)
            {
                var localPubDao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();
                var trackMetadataDao = new TrackMetadataDao(localPubDao, new HttpContentProvider());
                source = RemotePlaybackSource.CreateFromParcel(trackMetadataDao, (RemotePlaybackSourceParcel)parcel);
            }
            else if (parcel is MixedPlaybackSourceParcel)
            {
                var localPubDao = (TrackPublicationsDao)DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();
                var trackMetadataDao = new TrackMetadataDao(localPubDao, new HttpContentProvider());
                source = MixedPlaybackSource.CreateFromParcel(trackMetadataDao, (MixedPlaybackSourceParcel)parcel);
            }

            if (source == null) 
            {
                AmwLog.Error(LogTag, string.Format("can't enqueue \"{0}\"", parcel.GetType()));
                return;
            }

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

        public void OnTrackMetadataResolved(ITrackMetadata metadata)
        {
            AmwLog.Debug(LogTag, "playing track metadata resolved, updating play counters..");
            long? trackId = _player.GetCurrentTrackId();
            if (trackId != null)
            {
                _playCountDao.UpdatePlayCount((long)trackId);
            }
            else
            {
                _playCountDao.UpdatePlayCount(_player.GetCurrentTrackGuid());
            }

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

        public ITrackMetadata GetTrackMetadata()
        {
            return _player.GetTrackMetadata();
        }
    }
}