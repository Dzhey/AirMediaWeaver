
using System;
using AirMedia.Platform.Controller.PlaybackSource;
using AirMedia.Platform.Player.MediaService;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Player
{
    public static class PlayerControl
    {
        public static void Play(long[] trackIds, int position = 0, bool fastForward = true)
        {
            App.Instance.StartService(CreateEnqueueIntent(position, trackIds));

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraFastForward, true);
            App.Instance.StartService(intent);
        }

        public static void Play(long trackId, bool fastForward = true)
        {
            App.Instance.StartService(CreateEnqueueIntent(0, trackId));

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraFastForward, true);
            App.Instance.StartService(intent);
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
    }
}