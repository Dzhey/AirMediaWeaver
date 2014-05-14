
using System;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Data;
using AirMedia.Platform.Logger;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Player
{
    [Service(Exported = false, Enabled = true), 
    IntentFilter(new[] { ActionPlay, ActionStop })]
    public class MediaPlayerService : Service, 
        MediaPlayer.IOnPreparedListener, 
        MediaPlayer.IOnErrorListener, 
        MediaPlayer.IOnCompletionListener,
        MediaPlayer.IOnInfoListener,
        MediaPlayer.IOnSeekCompleteListener
    {
        public const string ExtraTrackId = "track_id";
        public const string ActionPlay = "org.eb.airmedia.android.intent.action.PLAY";
        public const string ActionStop = "org.eb.airmedia.android.intent.action.STOP";
        public const string ActionPause = "org.eb.airmedia.android.intent.action.PAUSE";
        public const string ActionUnpause = "org.eb.airmedia.android.intent.action.UNPAUSE";

        private const int MediaPlayerErrorUnknown = 38;

        private static readonly string LogTag = typeof (MediaPlayerService).Name;

        private MediaPlayer _player;
        private MediaPlayerBinder _binder;
        private TrackMetadata? _trackMetadata;
        private PlaybackStatus _playbackStatus;
        private RequestResultListener _requestResultListener;

        public override void OnCreate()
        {
            base.OnCreate();

            _binder = new MediaPlayerBinder(this);

            AmwLog.Debug(LogTag, "MediaPlayerService created");

            int random = new Random().Next(int.MaxValue);
            string listenerTag = string.Format("{0}_{1}", typeof (MediaPlayerService).Name, random);
            _requestResultListener = new RequestResultListener(listenerTag);
            _requestResultListener.RegisterResultHandler(
                typeof(ResolveMetadataRequest), OnResolveMetadataRequestResult);
        }

        public override void OnDestroy()
        {
            _requestResultListener.RemoveResultHandler(typeof(ResolveMetadataRequest));

            ReleasePlayer();
            _binder = null;
            _trackMetadata = null;

            base.OnDestroy();

            AmwLog.Debug(LogTag, "MediaPlayerService destroyed");
        }

        public override StartCommandResult OnStartCommand(Intent intent, 
            StartCommandFlags flags, int startId)
        {
            switch (intent.Action)
            {
                case ActionPlay:
                    long trackId = intent.GetLongExtra(ExtraTrackId, -1);
                    StartPlayback(intent.Data, trackId);
                    break;

                case ActionStop:
                    ReleasePlayer();
                    StopSelf();
                    break;

                case ActionPause:
                    Pause();
                    break;

                case ActionUnpause:
                    Unpause();
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format(
                        "Unhandled intent received \"{0}\"", intent.Action));
                    break;
            }

            return StartCommandResult.Sticky;
        }

        public bool Pause()
        {
            if (IsPlaying() == false) return false;
            _player.Pause();
            SetPlaybackStatus(PlaybackStatus.Paused);

            return true;
        }

        public bool Unpause()
        {
            if (_player != null && _player.IsPlaying == false)
            {
                _player.Start();
                SetPlaybackStatus(PlaybackStatus.Playing);

                return true;
            }

            return false;
        }

        public bool SeekTo(int location)
        {
            if (_player != null 
                && (_playbackStatus == PlaybackStatus.Playing
                    || _playbackStatus == PlaybackStatus.Paused))
            {
                _player.SeekTo(location);

                return true;
            }

            return false;
        }

        public bool IsPlaying()
        {
            if (_player == null) return false;

            return _player.IsPlaying;
        }

        public int GetCurrentPosition()
        {
            if (_player == null) return 0;

            return _player.CurrentPosition;
        }

        public int GetDuration()
        {
            if (_player == null) return 0;

            return _player.Duration;
        }

        public TrackMetadata? GetTrackMetadata()
        {
            return _trackMetadata;
        }

        public override IBinder OnBind(Intent intent)
        {
            return _binder;
        }

        private void StartPlayback(Uri uri, long trackId)
        {
            InitPlayer(false);

            if (uri == null)
            {
                AmwLog.Error(LogTag, "Uri to prepare playback is not specified");
            }

            AmwLog.Debug(LogTag, string.Format("preparing \"{0}\" to play..", uri));

            SetPlaybackStatus(PlaybackStatus.Preparing);

             _requestResultListener.SubmitRequest(new ResolveMetadataRequest(trackId));

            _player.SetDataSource(this, uri);
            _player.PrepareAsync();
        }

        public void OnPrepared(MediaPlayer mp)
        {
            AmwLog.Debug(LogTag, "MediaPlayer prepared, performing playback..");

            SetPlaybackStatus(PlaybackStatus.Playing);

            mp.Start();
        }

        public bool OnInfo(MediaPlayer mp, MediaInfo what, int extra)
        {
            return false;
        }

        public bool OnError(MediaPlayer mp, MediaError what, int extra)
        {
            AmwLog.Error(LogTag, string.Format("received MediaPlayer error:" +
                                               " \"{0}\"; extra: \"{1}\"", what, extra));

            if (MediaPlayerErrorUnknown == (int) what)
            {
                // TODO: retry
                AmwLog.Debug(LogTag, "retrying to play");
            }

            ReleasePlayer();

            return true;
        }

        public void OnSeekComplete(MediaPlayer mp)
        {
            _binder.NotifySeekCompleted();
        }

        public void OnCompletion(MediaPlayer mp)
        {
            AmwLog.Info(LogTag, "playback completed");
            ReleasePlayer();
            StopSelf();
        }

        private void InitPlayer(bool publishStatusUpdate)
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
            _player.SetOnSeekCompleteListener(this);

            SetPlaybackStatus(PlaybackStatus.Stopped, publishStatusUpdate);
        }

        private void ReleasePlayer()
        {
            if (_player == null) return;

            SetPlaybackStatus(PlaybackStatus.Stopped);

            _player.Reset();
            _player = null;
        }

        private void SetPlaybackStatus(PlaybackStatus status, bool publishUpdate = true)
        {
            if (status == _playbackStatus) return;

            _playbackStatus = status;

            switch (_playbackStatus)
            {
                case PlaybackStatus.Stopped:
                    if (publishUpdate) _binder.NotifyPlaybackStopped();
                    break;

                case PlaybackStatus.Playing:
                    if (publishUpdate) _binder.NotifyPlaybackStarted();
                    break;

                case PlaybackStatus.Preparing:
                case PlaybackStatus.Paused:
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format("undefined playback " +
                                                       "status set \"{0}\"", _playbackStatus));
                    break;
            }
        }

        private void OnResolveMetadataRequestResult(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, string.Format("error retrieving track metadata"));

                return;
            }

            _trackMetadata = ((LoadRequestResult<TrackMetadata?>) (args.Result)).Data;
            if (_trackMetadata == null)
            {
                AmwLog.Error(LogTag, "returned track metadata is empty");

                return;
            }

            AmwLog.Debug(LogTag, string.Format("track \"{0}\" metadata " +
                                               "successfuly resolved", _trackMetadata.Value.TrackTitle));
            if (_binder != null)
            {
                _binder.NotifyTrackMetadataResolved(_trackMetadata.Value);
            }
        }
    }
}