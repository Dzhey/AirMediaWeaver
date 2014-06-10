using AirMedia.Platform.Data;
using Android.Content;
using Android.Provider;

namespace AirMedia.Platform.Player
{
    public static class MetadataResolver
    {
        private static readonly string[] Projection = new[]
            {
                MediaStore.Audio.Media.InterfaceConsts.Id,
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.Album,
                MediaStore.Audio.Media.InterfaceConsts.Duration,
                MediaStore.Audio.Media.InterfaceConsts.Data
            };

        public static TrackMetadata? ResolveMetadata(long trackId)
        {
            var cr = App.Instance.ContentResolver;
            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, trackId);
            using (var cursor = cr.Query(uri, Projection, null, null, null))
            {
                try
                {
                    if (cursor.MoveToFirst() == false) return null;

                    var result = new TrackMetadata
                        {
                            TrackId = cursor.GetLong(0),
                            TrackTitle = cursor.GetString(1),
                            Artist = cursor.GetString(2),
                            Album = cursor.GetString(3),
                            TrackDurationMillis = cursor.GetInt(4),
                            Data = cursor.GetString(5)
                        };

                    return result;
                }
                finally
                {
                    cursor.Close();
                }
            }
        }
    }
}