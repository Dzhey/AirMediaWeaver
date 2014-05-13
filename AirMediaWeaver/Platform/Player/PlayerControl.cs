
using Android.Content;
using Android.Net;

namespace AirMedia.Platform.Player
{
    public static class PlayerControl
    {
        public static bool Play(long trackId)
        {
            var metadata = MetadataResolver.ResolveMetadata(trackId);

            if (metadata == null) return false;

            var intent = new Intent(MediaPlayerService.ActionPlay);
            intent.SetClass(App.Instance, typeof (MediaPlayerService));
            intent.PutExtra(MediaPlayerService.ExtraTrackId, metadata.Value.TrackId);
            intent.SetData(Uri.Parse(metadata.Value.Data));
            App.Instance.StartService(intent);

            return true;
        }

        public static void Stop()
        {
            var intent = new Intent(MediaPlayerService.ActionStop);
            intent.SetClass(App.Instance, typeof(MediaPlayerService));
            App.Instance.StartService(intent);
        }
    }
}