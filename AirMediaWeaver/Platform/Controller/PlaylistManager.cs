using System.Collections.Generic;
using AirMedia.Core.Data;
using Android.Provider;

namespace AirMedia.Platform.Controller
{
    public class PlaylistManager
    {
        private static readonly string[] PlaylistsQueryProjection = new[]
            {
                MediaStore.Audio.Playlists.InterfaceConsts.Id,
                MediaStore.Audio.Playlists.InterfaceConsts.Data,
                MediaStore.Audio.Playlists.InterfaceConsts.Name,
                MediaStore.Audio.Playlists.InterfaceConsts.DateAdded,
                MediaStore.Audio.Playlists.InterfaceConsts.DateModified
            };

        public static List<PlaylistModel> GetSystemPlaylists()
        {
            var resolver = App.Instance.ContentResolver;
            const string orderBy = MediaStore.Audio.Playlists.InterfaceConsts.Name;
            var cursor = resolver.Query(MediaStore.Audio.Playlists.ExternalContentUri,
                PlaylistsQueryProjection, null, null, orderBy);

            var playlists = new List<PlaylistModel>(cursor.Count);

            using (cursor)
            {
                while (cursor.MoveToNext())
                {
                    playlists.Add(new PlaylistModel
                        {
                            Id = cursor.GetLong(0),
                            Data = cursor.GetString(1),
                            Name = cursor.GetString(2),
                            DateAdded = cursor.GetLong(3),
                            DateModified = cursor.GetLong(4)
                        });
                }
            }

            return playlists;
        }
    }
}