using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform.Data;
using Android.Content;
using Android.Database;
using Android.Provider;
using Java.Lang;

namespace AirMedia.Platform.Controller
{
    public class PlaylistDao
    {
        private static readonly string LogTag = typeof (PlaylistDao).Name;

        public const string SortByArtist = MediaStore.Audio.Media.InterfaceConsts.Artist + " ASC";
        public const string SortByTitle = MediaStore.Audio.Media.InterfaceConsts.Title + " ASC";
        public const string SortByAlbum = MediaStore.Audio.Media.InterfaceConsts.Album + " ASC";

        public static readonly string DefaultTrackSortOrder = string.Join(",", SortByArtist, SortByTitle, SortByAlbum);

        private static readonly string[] PlaylistsQueryProjection = new[]
            {
                MediaStore.Audio.Playlists.InterfaceConsts.Id,
                MediaStore.Audio.Playlists.InterfaceConsts.Data,
                MediaStore.Audio.Playlists.InterfaceConsts.Name,
                MediaStore.Audio.Playlists.InterfaceConsts.DateAdded,
                MediaStore.Audio.Playlists.InterfaceConsts.DateModified
            };

        /// <summary>
        /// Keep both track metadata projections in sync.
        /// </summary>
        private static readonly string[] PlaylistTrackMetadataQueryProjection = new[]
            {
                MediaStore.Audio.Playlists.Members.AudioId,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Title,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Artist,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Album,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Duration,
                MediaStore.Audio.Playlists.Members.InterfaceConsts.Data
            };

        /// <summary>
        /// Keep both track metadata projections in sync.
        /// </summary>
        private static readonly string[] TrackMetadataQueryProjection = new[]
            {
                MediaStore.Audio.Media.InterfaceConsts.Id,
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.Album,
                MediaStore.Audio.Media.InterfaceConsts.Duration,
                MediaStore.Audio.Media.InterfaceConsts.Data,
                MediaStore.Audio.Media.InterfaceConsts.IsMusic
            };

        public static List<TrackMetadata> GetSystemTracks()
        {
            const string selectionTemplate = "{cIsMusic}=1";

            var selection = selectionTemplate.HaackFormat(new
                {
                    cIsMusic = MediaStore.Audio.Media.InterfaceConsts.IsMusic
                });
            var resolver = App.Instance.ContentResolver;
            var cursor = resolver.Query(MediaStore.Audio.Media.ExternalContentUri,
                                        TrackMetadataQueryProjection, selection, null, DefaultTrackSortOrder);

            var result = new List<TrackMetadata>(cursor.Count);

            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        result.Add(CreateTrackMetadata(cursor));
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return result;
        }

        public static TrackMetadata? GetTrackMetadata(long trackId)
        {
            try
            {
                var resolver = App.Instance.ContentResolver;
                var uri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, trackId);

                using (var cursor = resolver.Query(uri, TrackMetadataQueryProjection, null, null, null))
                {
                    try
                    {
                        if (cursor.MoveToFirst() == false) return null;

                        var metadata = CreateTrackMetadata(cursor);
                        metadata.Genre = TrackMetadataDao.QueryForAudioGenre((int)trackId);

                        return metadata;
                    }
                    finally
                    {
                        cursor.Close();
                    }
                } 
            }
            catch (Exception e)
            {
                AmwLog.Error(LogTag, e, "error retrieving track \"{0}\" metadata", trackId);
                throw;
            }
        }
        
        public static bool UpdatePlaylistContents(long playlistId, long[] trackIds)
        {
            ClearPlaylist(playlistId);

            if (trackIds.Length == 0) return true;

            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);

            var values = new ContentValues[trackIds.Length];
            for (int i = 0; i < trackIds.Length; i++)
            {
                var content = new ContentValues();
                content.Put(MediaStore.Audio.Playlists.Members.AudioId, trackIds[i]);
                content.Put(MediaStore.Audio.Playlists.Members.PlayOrder, i);
                values[i] = content;
            }

            int ret = resolver.BulkInsert(uri, values);

            return ret == trackIds.Length;
        }

        public static void ClearPlaylist(long playlistId)
        {
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            resolver.Delete(uri, null, null);
        }

        public static bool RemovePlaylists(params long[] playlistIds)
        {
            var resolver = App.Instance.ContentResolver;

            foreach (var playlistId in playlistIds)
            {
                var uri = ContentUris.WithAppendedId(MediaStore.Audio.Playlists.ExternalContentUri, playlistId);
                int ret = resolver.Delete(uri, null, null);

                if (ret < 1) return false;
            }

            return true;
        }

        public static bool RenamePlaylist(long playlistId, string playlistName)
        {
            var resolver = App.Instance.ContentResolver;

            var insertValues = new ContentValues();

            long date = JavaSystem.CurrentTimeMillis();
            insertValues.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, playlistName);
            insertValues.Put(MediaStore.Audio.Playlists.InterfaceConsts.DateModified, date);

            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Playlists.ExternalContentUri, playlistId);
            int ret = resolver.Update(uri, insertValues, null, null);

            return ret > 0;
        }

        public static PlaylistModel CreateNewPlaylist(string playlistName)
        {
            var resolver = App.Instance.ContentResolver;

            var insertValues = new ContentValues();

            long date = JavaSystem.CurrentTimeMillis();
            insertValues.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, playlistName);
            insertValues.Put(MediaStore.Audio.Playlists.InterfaceConsts.DateAdded, date);
            insertValues.Put(MediaStore.Audio.Playlists.InterfaceConsts.DateModified, date);

            var uri = resolver.Insert(MediaStore.Audio.Playlists.ExternalContentUri, insertValues);

            if (uri == null) return null;

            using (var cursor = resolver.Query(uri, PlaylistsQueryProjection, null, null, null))
            {
                try
                {
                    if (cursor.MoveToFirst())
                    {
                        return CreatePlaylist(cursor);
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return null;
        }

        public static PlaylistModel GetPlaylist(long playlistId)
        {
            var resolver = App.Instance.ContentResolver;

            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Playlists.ExternalContentUri, playlistId);

            var cursor = resolver.Query(uri, PlaylistsQueryProjection, null, null, null);

            using (cursor)
            {
                try
                {
                    if (cursor.MoveToFirst() == false) return null;

                    return CreatePlaylist(cursor);
                }
                finally
                {
                    cursor.Close();
                }
            }
        }

        public static List<TrackMetadata> GetPlaylistTracks(long playlistId)
        {
            const string orderBy = MediaStore.Audio.Playlists.Members.PlayOrder;

            var resolver = App.Instance.ContentResolver;

            var uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            var cursor = resolver.Query(uri, PlaylistTrackMetadataQueryProjection, null, null, orderBy);

            var metadata = new List<TrackMetadata>(cursor.Count);

            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        metadata.Add(CreateTrackMetadata(cursor));
                    }   
                }
                finally
                {
                    cursor.Close();
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
                try
                {
                    while (cursor.MoveToNext())
                    {
                        playlists.Add(CreatePlaylist(cursor));
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return playlists;
        }

        private static TrackMetadata CreateTrackMetadata(ICursor cursor)
        {
            return new TrackMetadata
                {
                    TrackId = cursor.GetLong(0),
                    TrackTitle = cursor.GetString(1),
                    Artist = cursor.GetString(2),
                    Album = cursor.GetString(3),
                    TrackDurationMillis = cursor.GetInt(4),
                    Data = cursor.GetString(5)
                };
        }

        private static PlaylistModel CreatePlaylist(ICursor cursor)
        {
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
}