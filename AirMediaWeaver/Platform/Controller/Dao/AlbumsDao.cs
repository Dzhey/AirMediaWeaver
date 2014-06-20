using System;
using AirMedia.Core.Log;
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

        public static Bitmap GetAlbumArt(long albumId)
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
                ParcelFileDescriptor pfd = null;
                try
                {
                    if (cursor.MoveToFirst() == false)
                        return null;

                    string albumArt = cursor.GetString(1);

                    if (string.IsNullOrEmpty(albumArt))
                        return null;

                    var artUri = Uri.Parse(ContentResolver.SchemeFile + "://" + albumArt);
                    pfd = resolver.OpenFileDescriptor(artUri, "r");

                    return BitmapFactory.DecodeFileDescriptor(pfd.FileDescriptor);
                }
                finally
                {
                    cursor.Close();
                    if (pfd != null)
                    {
                        pfd.Close();
                        pfd.Dispose();
                    }
                }
            }
        }
    }
}