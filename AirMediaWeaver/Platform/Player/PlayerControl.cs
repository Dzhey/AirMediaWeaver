
using System;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.PlaybackSource;
using AirMedia.Platform.Player.MediaService;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Player
{
    public static class PlayerControl
    {
        public static readonly string LogTag = typeof(PlayerControl).Name;

        public static void Rewind()
        {
            var intent = new Intent(MediaPlayerService.ActionRewind);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }

        public static void FastForward()
        {
            var intent = new Intent(MediaPlayerService.ActionFastForward);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }

        public static void Play(string[] trackGuids, int position = 0, bool fastForward = true)
        {
            App.Instance.StartService(CreateEnqueueIntent(position, trackGuids));

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraFastForward, true);
            App.Instance.StartService(intent);

            AmwLog.Debug(LogTag, string.Format("{0} remote tracks enqueued", trackGuids.Length));
        }

        public static void Play(long[] trackIds, int position = 0, bool fastForward = true)
        {
            App.Instance.StartService(CreateEnqueueIntent(position, trackIds));

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraFastForward, true);
            App.Instance.StartService(intent);

            AmwLog.Debug(LogTag, string.Format("{0} tracks enqueued", trackIds.Length));
        }

        public static void Play(long trackId, bool fastForward = true)
        {
            App.Instance.StartService(CreateEnqueueIntent(0, trackId));

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraFastForward, true);
            App.Instance.StartService(intent);
        }

        public static void Stop()
        {
            var intent = new Intent(MediaPlayerService.ActionStop);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }

        public static void Pause()
        {
            var intent = new Intent(MediaPlayerService.ActionPause);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }

        public static void Unpause()
        {
            var intent = new Intent(MediaPlayerService.ActionUnpause);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }

        private static Intent CreateEnqueueIntent(int position, params string[] trackGuids)
        {
            if (trackGuids.Length < 1)
            {
                throw new ApplicationException("no track guids provided");
            }

            var intent = new Intent(MediaPlayerService.ActionEnqueue);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));

            var parcelSource = new RemotePlaybackSource(null, position, trackGuids).CreateParcelSource();

            intent.PutExtra(MediaPlayerService.ExtraPlaybackSource, parcelSource);

            return intent;
        }

        private static Intent CreateEnqueueIntent(int position, params long[] trackIds)
        {
            if (trackIds.Length < 1)
            {
                throw new ApplicationException("no trackIds provided");
            }

            var intent = new Intent(MediaPlayerService.ActionEnqueue);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));

            IParcelable parcelable;
            if (trackIds.Length == 1)
            {
                parcelable = new SingleLocalPlaybackSource(trackIds[0]).CreateParcelSource();
            }
            else
            {
                parcelable = new LocalLibraryPlaybackSource(position, trackIds).CreateParcelSource();
            }

            intent.PutExtra(MediaPlayerService.ExtraPlaybackSource, parcelable);

            return intent;
        }
    }
}