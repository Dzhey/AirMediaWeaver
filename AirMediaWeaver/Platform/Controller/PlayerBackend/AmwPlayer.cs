using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.PlaybackSource;
using AirMedia.Platform.Controller.Requests;
using AirMedia.Platform.Logger;
using AirMedia.Platform.Player.MediaService;
using Android.Media;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Controller.PlayerBackend
{
    public class AmwPlayer :  Java.Lang.Object, 
        MediaPlayer.IOnPreparedListener, 
        MediaPlayer.IOnErrorListener, 
        MediaPlayer.IOnCompletionListener,
        MediaPlayer.IOnInfoListener,
        MediaPlayer.IOnSeekCompleteListener,
        MediaPlayer.IOnBufferingUpdateListener
    {
        private const int MediaPlayerRetryCount = 2;
        private const int MediaPlayerErrorUnknown = -38;

        private static readonly string LogTag = typeof(AmwPlayer).Name;

        public IAmwPlayerCallbacks Callbacks { get; set; }
        public PlaybackStatus Status
        {
            get
            {
                return _playbackStatus;
            }
        }

        private MediaPlayer _player;
        private readonly QueuePlaybackSource _queue;
        private PlaybackStatus _playbackStatus;
        private ITrackMetadata _trackMetadata;
        private readonly RequestResultListener _requestResultListener;
        private int _retryCount;

        public AmwPlayer()
        {
            _requestResultListener = RequestResultListener.NewInstance(typeof (MediaPlayerService).Name);
            _requestResultListener.RegisterResultHandler(
                typeof(LoadTrackMetadataRequest), OnResolveMetadataRequestResult);
            
            _queue = new QueuePlaybackSource();
            _playbackStatus = PlaybackStatus.Stopped;
        }

        public ITrackMetadata GetTrackMetadata()
        {
            return _trackMetadata;
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
                AmwLog.Debug(LogTag, string.Format("seekTo({0})", location / 1000));
                _player.SeekTo(location);
                AmwLog.Debug(LogTag, "seekTo done)");

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
            if (_player == null || (Status != PlaybackStatus.Paused 
                && Status != PlaybackStatus.Playing)) return 0;

            return _player.Duration;
        }

        public void ResetQueue()
        {
            _queue.Reset();
        }

        public void Enqueue(IBasicPlaybackSource source)
        {
            _queue.EnqueueSource(source);
        }

        public void Rewind()
        {
            if (IsPlaying())
            {
                _player.Stop();
            }

            _queue.MovePrevious();
            if (Play() == false)
            {
                ReleasePlayer();
            }
        }

        public void FastForward()
        {
            if (IsPlaying())
            {
                _player.Stop();
            }

            _queue.MoveNext();
            if (Play() == false)
            {
                ReleasePlayer();
            }
        }

        public void Stop()
        {
            AmwLog.Debug(LogTag, "Stop()");
            if (IsPlaying() || _playbackStatus == PlaybackStatus.Paused)
            {
                _player.Stop();

                ReleasePlayer();
            }
            AmwLog.Debug(LogTag, "after Stop()");
        }

        public bool Play()
        {
            if (IsPlaying())
            {
                AmwLog.Warn(LogTag,"Play(): already playing");
                return false;
            }

            if (_queue.HasCurrent() == false)
            {
                AmwLog.Debug(LogTag, "Play(): no track to play");
                return false;
            }

            var track = _queue.GetCurrentResource();

            if (track == null || track.Value.Uri == null)
            {
                AmwLog.Error(LogTag, "Play(): can't fetch next playback resource");
                return false;
            }

            var uri = Uri.Parse(track.Value.Uri.ToString());
            AmwLog.Debug(LogTag, string.Format("Preparing \"{0}\"", uri));

            InitPlayer(false);

            SetPlaybackStatus(PlaybackStatus.Preparing);

            _requestResultListener.SubmitRequest(new LoadTrackMetadataRequest(
                track.Value.LocalId, track.Value.PublicGuid));

            _player.SetDataSource(App.Instance, uri);
            _player.PrepareAsync();

            return true;
        }

        public void OnPrepared(MediaPlayer mp)
        {
            AmwLog.Debug(LogTag, "MediaPlayer prepared, starting playback..");

            SetPlaybackStatus(PlaybackStatus.Playing);

            mp.Start();

            _retryCount = 0;
        }

        public bool OnInfo(MediaPlayer mp, MediaInfo what, int extra)
        {
            AmwLog.Verbose(LogTag, string.Format("mediaplayer info ({0},{1})", what, extra));

            return false;
        }

        public bool OnError(MediaPlayer mp, MediaError what, int extra)
        {
            AmwLog.Warn(LogTag, string.Format("received MediaPlayer error:" +
                                               " \"{0}\"; extra: \"{1}\"", what, extra));
            ReleasePlayer();

            if (MediaPlayerErrorUnknown == (int)what)
            {
                if (_retryCount == MediaPlayerRetryCount)
                {
                    AmwLog.Error(LogTag, "retry count exceeded, stopping");
                }
                else
                {
                    _retryCount++;
                    AmwLog.Debug(LogTag, "retrying to play..");
                    App.MainHandler.PostDelayed(() => Play(), 200);

                    return true;
                }
            }

            if (Callbacks != null)
            {
                Callbacks.OnPlaybackCompleted();
            }

            return true;
        }

        public void OnSeekComplete(MediaPlayer mp)
        {
            AmwLog.Debug(LogTag, "OnSeekComplete()");
            if (Callbacks != null)
            {
                Callbacks.OnSeekComplete();
            }
            AmwLog.Debug(LogTag, "after OnSeekComplete()");
        }

        public void OnBufferingUpdate(MediaPlayer mp, int percent)
        {
            AmwLog.Verbose(LogTag, string.Format("media player buffering: {0}%", percent));
        }

        public void OnCompletion(MediaPlayer mp)
        {
            AmwLog.Debug(LogTag, "playback completed; starting next track..");

            if (!_queue.MoveNext() || !Play())
            {
                AmwLog.Debug(LogTag, "playback completed; no more tracks to play");
                ReleasePlayer();
                if (Callbacks != null)
                {
                    Callbacks.OnPlaybackCompleted();
                }
            }
        }

        public void Release()
        {
            AmwLog.Debug(LogTag, "Release() called");
            ReleasePlayer();

            _requestResultListener.RemoveResultHandler(typeof(LoadTrackMetadataRequest));
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
            _player.SetOnBufferingUpdateListener(this);

            SetPlaybackStatus(PlaybackStatus.Stopped, publishStatusUpdate);
        }

        private void ReleasePlayer()
        {
            if (_player == null)
            {
                AmwLog.Debug(LogTag, "player is already released");
                return;
            }

            AmwLog.Debug(LogTag, "releasing media player");

            SetPlaybackStatus(PlaybackStatus.Stopped);

            AmwLog.Debug(LogTag, "playback status changed to stopped");

            _player.Reset();
            _player.Release();
            _player = null;
            AmwLog.Debug(LogTag, "player released");
        }

        private void SetPlaybackStatus(PlaybackStatus status, bool publishUpdate = true)
        {
            if (status == _playbackStatus) return;

            AmwLog.Debug(LogTag, string.Format("changing status ({0}, {1})", _playbackStatus, status));
            _playbackStatus = status;

            switch (_playbackStatus)
            {
                case PlaybackStatus.Stopped:
                    if (publishUpdate && Callbacks != null)
                    {
                        Callbacks.OnPlaybackStopped();
                    }
                    break;

                case PlaybackStatus.Playing:
                    if (publishUpdate && Callbacks != null)
                    {
                        Callbacks.OnPlaybackStarted();
                    }
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

            _trackMetadata = ((LoadRequestResult<ITrackMetadata>)(args.Result)).Data;
            if (_trackMetadata == null)
            {
                AmwLog.Error(LogTag, "returned track metadata is empty");

                return;
            }

            AmwLog.Debug(LogTag, string.Format("track \"{0}\" metadata " +
                                               "successfuly resolved", _trackMetadata.TrackTitle));

            if (Callbacks != null)
            {
                Callbacks.OnTrackMetadataResolved(_trackMetadata);
            }
        }
    }
}