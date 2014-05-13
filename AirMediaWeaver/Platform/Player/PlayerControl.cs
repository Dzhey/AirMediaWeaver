
using Android.Content;
using Android.Net;

namespace AirMedia.Platform.Player
{
    public static class PlayerControl
    {
        public static void Play(Uri resource)
        {
            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof (MediaPlayerService));
            intent.SetData(resource);
            App.Instance.StartService(intent);
        }

        public static void Stop()
        {
            var intent = new Intent(MediaPlayerService.ActionStop);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }
    }
}