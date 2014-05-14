using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Platform.Data;
using Android.Content;
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

        private static readonly string[] PlaylistTracksQueryProjection = new[]
            {
                MediaStore.Audio.Playlists.Members.AudioId,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Title,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Artist,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Album,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Duration,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Data
            };

        public static PlaylistModel GetPlaylist(long playlistId)
        {
            var resolver = App.Instance.ContentResolver;

            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Playlists.ExternalContentUri, playlistId);

            var cursor = resolver.Query(uri, PlaylistsQueryProjection, null, null, null);

            using (cursor)
            {
                if (cursor.MoveToFirst() == false) return null;

                return new PlaylistModel
                {
                    Id = cursor.GetLong(0),
                    Data = cursor.GetString(1),
                    Name = cursor.GetString(2),
                    DateAdded = cursor.GetLong(3),
                    DateModified = cursor.GetLong(4)
                };
            }
        }

        public static List<TrackMetadata> GetPlaylistTracks(long playlistId)
        {
            const string orderBy = MediaStore.Audio.Playlists.Members.PlayOrder;

            var resolver = App.Instance.ContentResolver;

            var uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            var cursor = resolver.Query(uri, PlaylistTracksQueryProjection, null, null, orderBy);

            var metadata = new List<TrackMetadata>(cursor.Count);

            using (cursor)
            {
                while (cursor.MoveToNext())
                {
                    metadata.Add(new TrackMetadata
                        {
                            TrackId = cursor.GetLong(0),
                            TrackTitle = cursor.GetString(1),
                            ArtistName = cursor.GetString(2),
                            Album = cursor.GetString(3),
                            Duration = cursor.GetInt(4),
                            Data = cursor.GetString(5)
                        });
                }
            }

            return metadata;
        }

        public static List<PlaylistModel> GetSystemPlaylists()
        {
            const string orderBy = MediaStore.Audio.Playlists.InterfaceConsts.Name;

            var resolver = App.Instance.ContentResolver;
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