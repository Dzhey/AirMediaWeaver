using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Controller.Dao
{
    public static class AlbumsDao
    {
        private static readonly string LogTag = typeof(AlbumsDao).Name;

        public static string GetAlbumArtPath(long albumId)
        {
            var resolver = App.Instance.ContentResolver;
            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Albums.ExternalContentUri, albumId);

            var cursor = resolver.Query(uri,
                new[]
                {
                    MediaStore.Audio.Albums.InterfaceConsts.Id,
                    MediaStore.Audio.Albums.InterfaceConsts.AlbumArt

                }, null, null, null);

            using (cursor)
            {
                try
                {
                    if (cursor.MoveToFirst() == false)
                        return null;

                    string albumArt = cursor.GetString(1);

                    return albumArt;
                }
                finally
                {
                    cursor.Close();
                }
            }
        }

        public static Bitmap GetAlbumArtBitmap(long albumId, BitmapFactory.Options options = null)
        {
            ParcelFileDescriptor pfd = null;
            try
            {
                string albumArt = GetAlbumArtPath(albumId);

                if (string.IsNullOrEmpty(albumArt))
                    return null;

                var artUri = Uri.Parse(ContentResolver.SchemeFile + "://" + albumArt);
                pfd = App.Instance.ContentResolver.OpenFileDescriptor(artUri, "r");

                return BitmapFactory.DecodeFileDescriptor(pfd.FileDescriptor, null, options);
            }
            finally
            {
                if (pfd != null)
                {
                    pfd.Close();
                    pfd.Dispose();
                }
            }
        }
    }
}