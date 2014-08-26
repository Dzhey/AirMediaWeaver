using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Model;
using AirMedia.Platform.Data.Sql;
using Android.Provider;

namespace AirMedia.Core.Data
{
    public class TrackMetadataDao : ITrackMetadataDao
    {
        public static readonly string LogTag = typeof(TrackMetadataDao).Name;

        private static readonly string[] GenresQueryProjection = new[]
            {
                MediaStore.Audio.Genres.InterfaceConsts.Id,
                MediaStore.Audio.Genres.InterfaceConsts.Name
            };

        private static readonly string[] ArtistsBaseQueryProjection = new[]
            {
                MediaStore.Audio.Artists.InterfaceConsts.Id,
                MediaStore.Audio.Artists.InterfaceConsts.Artist
            };

        private static readonly string[] AlbumsBaseQueryProjection = new[]
            {
                MediaStore.Audio.Albums.InterfaceConsts.Id,
                MediaStore.Audio.Albums.InterfaceConsts.Album
            };

        private static readonly string[] PlaylistTrackMetadataQueryProjection = new[]
            {
                MediaStore.Audio.Media.InterfaceConsts.Id,
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.Album,
                MediaStore.Audio.Media.InterfaceConsts.Duration,
                MediaStore.Audio.Media.InterfaceConsts.Data
            };

        private readonly IAmwContentProvider _amwContentProvider;
        private readonly ITrackPublicationsDao _localPubDao;
        private readonly RemoteTrackPublicationsDao _pubDao;

        public TrackMetadataDao(ITrackPublicationsDao localPubDao, IAmwContentProvider amwContentProvider)
        {
            _localPubDao = localPubDao;
            _amwContentProvider = amwContentProvider;
            _pubDao = (RemoteTrackPublicationsDao) DatabaseHelper.Instance
                                                                 .GetDao<RemoteTrackPublicationRecord>();

        }

        public IRemoteTrackMetadata[] QueryRemoteTracksForTitleLike(string trackTitle)
        {
            return QueryRemoteTracksForColumnLike(RemoteTrackPublicationRecord.ColumnTitle, trackTitle);
        }

        public IRemoteTrackMetadata[] QueryRemoteTracksForGenreNameLike(string genreName)
        {
            return QueryRemoteTracksForColumnLike(RemoteTrackPublicationRecord.ColumnGenre, genreName);
        }

        public ArtistBaseModel[] QueryForLocalArtists()
        {
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Artists.ExternalContentUri;
            var cursor = resolver.Query(uri, ArtistsBaseQueryProjection, null, null, PlaylistDao.SortByArtist);
            var items = new ArtistBaseModel[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        items[cursor.Position] = new ArtistBaseModel
                            {
                                ArtistId = cursor.GetLong(0),
                                ArtistName = cursor.GetString(1)
                            };
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return items;
        }

        public AlbumBaseModel[] QueryForArtistAlbums(long artistId)
        {
            const string orderBy = MediaStore.Audio.Albums.InterfaceConsts.Album + " ASC";
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Artists.Albums.GetContentUri("external", artistId);
            var cursor = resolver.Query(uri, AlbumsBaseQueryProjection, null, null, orderBy);
            var items = new AlbumBaseModel[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        items[cursor.Position] = new AlbumBaseModel
                        {
                            AlbumId = cursor.GetLong(0),
                            AlbumName = cursor.GetString(1)
                        };
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return items;
        }

        public AlbumBaseModel[] QueryForLocalAlbums()
        {
            const string orderBy = MediaStore.Audio.Albums.InterfaceConsts.Album + " ASC";
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Albums.ExternalContentUri;
            var cursor = resolver.Query(uri, AlbumsBaseQueryProjection, null, null, orderBy);
            var items = new AlbumBaseModel[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        items[cursor.Position] = new AlbumBaseModel
                        {
                            AlbumId = cursor.GetLong(0),
                            AlbumName = cursor.GetString(1)
                        };
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return items;
        }

        public IRemoteTrackMetadata[] QueryRemoteTracksForAlbumNameLike(string albumName)
        {
            return QueryRemoteTracksForColumnLike(RemoteTrackPublicationRecord.ColumnAlbum, albumName);
        }

        public IRemoteTrackMetadata[] QueryRemoteTracksForArtistNameLike(string artistName)
        {
            return QueryRemoteTracksForColumnLike(RemoteTrackPublicationRecord.ColumnArtist, artistName);
        }

        public ITrackMetadata[] QueryLocalTracksForGenreNameLike(string genreName)
        {
            var matchedGenres = QueryLocalGenresForNameLike(genreName);

            if (matchedGenres.Length == 0) return new ITrackMetadata[0];

            var resolver = App.Instance.ContentResolver;
            var matchedAudios = new HashSet<ITrackMetadata>();
            foreach (var genre in matchedGenres)
            {
                var uri = MediaStore.Audio.Genres.Members.GetContentUri("external", genre.GenreId);
                var cursor = resolver.Query(uri, PlaylistTrackMetadataQueryProjection, null, null, null);
                using (cursor)
                {
                    try
                    {
                        while (cursor.MoveToNext())
                        {
                            matchedAudios.Add(new TrackMetadata
                                {
                                    TrackId = cursor.GetLong(0),
                                    TrackTitle = cursor.GetString(1),
                                    Artist = cursor.GetString(2),
                                    Album = cursor.GetString(3),
                                    TrackDurationMillis = cursor.GetInt(4),
                                    Data = cursor.GetString(5)
                                });
                        }
                    }
                    finally
                    {
                        cursor.Close();
                    }
                }
            }

            return matchedAudios.ToArray();
        }

        public ITrackMetadata[] QueryLocalTracksForTitleLike(string trackTitle)
        {
            return QueryLocalTracksForColumnNameLike(MediaStore.Audio.Media.InterfaceConsts.Title, trackTitle);
        }

        public ITrackMetadata[] QueryLocalTracksForAlbumNameLike(string albumName)
        {
            return QueryLocalTracksForColumnNameLike(MediaStore.Audio.Media.InterfaceConsts.Album, albumName);
        }

        public ITrackMetadata[] QueryLocalTracksForArtistNameLike(string artistName)
        {
            return QueryLocalTracksForColumnNameLike(MediaStore.Audio.Media.InterfaceConsts.Artist, artistName);
        }

        public static long[] GetLocalLibraryTrackIds()
        {
            var resolver = App.Instance.ContentResolver;
            var cursor = resolver.Query(MediaStore.Audio.Media.ExternalContentUri,
                                        new[]
                                            {
                                                MediaStore.Audio.Media.InterfaceConsts.Id,
                                                MediaStore.Audio.Media.InterfaceConsts.IsMusic,
                                            },
                                        string.Format("{0}=1", MediaStore.Audio.Media.InterfaceConsts.IsMusic), 
                                        null, null);

            var trackIds = new long[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        trackIds[cursor.Position] = cursor.GetLong(0);
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return trackIds;
        }

        public static TrackMetadata[] CreateTracksMetadata<T>(IEnumerable<T> records) where T : ITrackMetadata
        {
            return records.Select(CreateTrackMetadata).ToArray();
        }

        public static TrackMetadata CreateTrackMetadata<T>(T record) where T : ITrackMetadata
        {
            return new TrackMetadata
            {
                TrackGuid = record.TrackGuid,
                PeerGuid = record.PeerGuid,
                Album = record.Album,
                Artist = record.Artist,
                TrackDurationMillis = record.TrackDurationMillis,
                TrackTitle = record.TrackTitle
            };
        }

        public static RemoteTrackPublicationRecord[] CreateRemotePublicationsRecord(
            IEnumerable<ITrackMetadata> records)
        {
            return records.Select(CreateRemotePublicationRecord).ToArray();
        }

        public static RemoteTrackPublicationRecord CreateRemotePublicationRecord(ITrackMetadata metadata)
        {
            return new RemoteTrackPublicationRecord
                {
                    TrackTitle = metadata.TrackGuid,
                    PeerGuid = metadata.PeerGuid,
                    Album = metadata.Album,
                    Artist = metadata.Artist,
                    TrackDurationMillis = metadata.TrackDurationMillis
                };
        }

        public static string[] QueryForAudioGenres(int audioId)
        {
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Genres.GetContentUriForAudioId("external", audioId);
            var cursor = resolver.Query(uri,
                new[] { MediaStore.Audio.Genres.InterfaceConsts.Name },
                null,
                null,
                null);

            var result = new string[cursor.Count];

            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        result[cursor.Position] = cursor.GetString(0);
                    }
                }
                finally
                {
                    cursor.Close();
                }

                return result;
            }
        }

        public static string QueryForAudioGenre(int audioId)
        {
            var resolver = App.Instance.ContentResolver;
            var uri = MediaStore.Audio.Genres.GetContentUriForAudioId("external", audioId);
            var cursor = resolver.Query(uri,
                new[] { MediaStore.Audio.Genres.InterfaceConsts.Name },
                null,
                null,
                null);

            using (cursor)
            {
                try
                {
                    if (cursor.MoveToNext())
                    {
                        return cursor.GetString(0);
                    }

                    return string.Empty;
                }
                finally
                {
                    cursor.Close();
                }
            }
        }

        public void UpdateMetadata(IEnumerable<ITrackMetadata> metadata)
        {
            _pubDao.RedefineDatabaseRecords(CreateRemotePublicationsRecord(metadata));
        }

        public Uri GetRemoteTrackUri(string trackGuid)
        {
            const string template = "select {tPeers_cPeerAddress} " +
                                    "from {tPeers} " +
                                    "where {tPeers_cPeerGuid} in (" +
                                        "select {tTracks_cPeerGuid} " +
                                        "from {tTracks} " +
                                        "where {tTracks_cTrackGuid}='{trackGuid}' " +
                                        "limit 1)" +
                                    "limit 1";

            string query = template.HaackFormat(new
                {
                    tPeers = PeerRecord.TableName,
                    tPeers_cPeerAddress = PeerRecord.ColumnAddress,
                    tPeers_cPeerGuid = PeerRecord.ColumnPeerGuid,
                    tTracks = RemoteTrackPublicationRecord.TableName,
                    tTracks_cTrackGuid = RemoteTrackPublicationRecord.ColumnTrackGuid, 
                    tTracks_cPeerGuid = RemoteTrackPublicationRecord.ColumnPeerGuid, 
                    trackGuid
                });

            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this))
            {
                var result = holder.Connection.Query<PeerRecord>(query).ToArray();

                if (result.Length == 0) return null;

                return _amwContentProvider.CreateRemoteTrackUri(
                    result[0].Address, 
                    Consts.DefaultHttpPort.ToString(CultureInfo.InvariantCulture), 
                    trackGuid);
            }
        }

        public ITrackMetadata GetTrackMetadata(string trackGuid)
        {
            var metadata = _localPubDao.QueryForGuid(trackGuid);

            if (metadata != null) return metadata;

            var pub = _pubDao.QueryForGuid(trackGuid);

            if (pub == null) return null;

            return CreateTrackMetadata(pub);
        }

        public IRemoteTrackMetadata[] GetRemoteTracksMetadata()
        {
            return _pubDao.GetAll().Select(item => new RemoteTrackPublicationRecord(item)).ToArray();
        }

        public IRemoteTrackMetadata GetRemoteTrackMetadata(string trackGuid)
        {
            var pub = _pubDao.QueryForGuid(trackGuid);

            if (pub == null) return null;

            return new RemoteTrackMetadata(pub);
        }

        public IRemoteTrackMetadata[] GetNotPlayedTracks()
        {
            var watch = new Stopwatch();
            watch.Start();
            var remoteTracks = GetNotPlayedRemoteTracks();
            watch.Stop();
            AmwLog.Debug(LogTag, string.Format("got not played remote tracks in {0} seconds",
                watch.ElapsedMilliseconds / 1000));

            watch.Reset();
            watch.Start();
            var localTracks = GetNotPlayedLocalTracks()
                .Select(metadata => new RemoteTrackMetadata(metadata))
                .ToArray();
            watch.Stop();

            AmwLog.Debug(LogTag, string.Format("got not played local tracks in {0} seconds",
                watch.ElapsedMilliseconds / 1000));

            return remoteTracks.Concat(localTracks).ToArray();
        }

        protected IRemoteTrackMetadata[] QueryRemoteTracksForColumnLike(string columnName, string columnValue)
        {
            const string template = "select * " +
                                    "from {tRemoteTracks} " +
                                    "where {cColumnName} " +
                                    "like ?";

            if (string.IsNullOrWhiteSpace(columnValue))
            {
                return new IRemoteTrackMetadata[0];
            }

            string query = template.HaackFormat(new
            {
                tRemoteTracks = RemoteTrackPublicationRecord.TableName,
                cColumnName = columnName
            });
            var args = new object[] { string.Format("%{0}%", columnValue.Replace(" ", "%")) };

            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this))
            {
                return holder.Connection.Query<RemoteTrackPublicationRecord>(query, args)
                                        .ConvertAll(input => (IRemoteTrackMetadata)input)
                                        .ToArray();
            }
        }

        protected GenreModel[] QueryLocalGenresForNameLike(string genreName)
        {
            var resolver = App.Instance.ContentResolver;
            var genresUri = MediaStore.Audio.Genres.ExternalContentUri;
            string selection = "{cGenreName} LIKE ?".HaackFormat(new
            {
                cGenreName = MediaStore.Audio.Genres.InterfaceConsts.Name
            });
            var cursor = resolver.Query(genresUri, GenresQueryProjection,
                selection, new[] { string.Format("%{0}%", genreName) }, null);

            var matchedGenres = new GenreModel[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        var genreModel = new GenreModel
                            {
                                GenreId = cursor.GetInt(0),
                                GenreName = cursor.GetString(1)
                            };
                        matchedGenres[cursor.Position] = genreModel;
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return matchedGenres;
        }

        protected ITrackMetadata[] QueryLocalTracksForColumnNameLike(string columnName, string columnValue)
        {
            if (string.IsNullOrWhiteSpace(columnValue))
            {
                return new ITrackMetadata[0];
            }

            var resolver = App.Instance.ContentResolver;

            string selection = "{cColumnName} LIKE ?".HaackFormat(new
            {
                cColumnName = columnName
            });

            var cursor = resolver.Query(MediaStore.Audio.Media.ExternalContentUri,
                                        PlaylistTrackMetadataQueryProjection,
                                        selection,
                                        new[] { string.Format("%{0}%", columnValue.Replace(" ", "%")) },
                                        PlaylistDao.DefaultTrackSortOrder);
            var result = new ITrackMetadata[cursor.Count];
            using (cursor)
            {
                try
                {
                    while (cursor.MoveToNext())
                    {
                        result[cursor.Position] = new TrackMetadata
                        {
                            TrackId = cursor.GetLong(0),
                            TrackTitle = cursor.GetString(1),
                            Artist = cursor.GetString(2),
                            Album = cursor.GetString(3),
                            TrackDurationMillis = cursor.GetInt(4),
                            Data = cursor.GetString(5)
                        };
                    }
                }
                finally
                {
                    cursor.Close();
                }
            }

            return result;
        }

        private IReadOnlyCollection<IRemoteTrackMetadata> GetNotPlayedRemoteTracks()
        {
            const string template = "select * " +
                                    "from {tRemoteTracks} " +
                                    "where {tRemoteTracks}.{tRemoteTracks_cTrackGuid} " +
                                    "not in (select {tPlayCount}.{tPlayCount_cTrackGuid} " +
                                                    "from {tPlayCount} " +
                                                    "where {tPlayCount_cPlayCount}>0)";
            string query = template.HaackFormat(new
                {
                    tRemoteTracks = RemoteTrackPublicationRecord.TableName,
                    tRemoteTracks_cTrackGuid = RemoteTrackPublicationRecord.ColumnTrackGuid,
                    tPlayCount = TrackPlayCountRecord.TableName,
                    tPlayCount_cTrackGuid = TrackPlayCountRecord.ColumnTrackGuid,
                    tPlayCount_cPlayCount = TrackPlayCountRecord.ColumnPlayCount
                });

            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this))
            {
                var result = holder.Connection
                                   .Query<RemoteTrackPublicationRecord>(query)
                                   .ToArray();

                return result;
            }
        }

        private IReadOnlyCollection<ITrackMetadata> GetNotPlayedLocalTracks()
        {
            var trackPlayCountDao = (TrackPlayCountDao)DatabaseHelper.Instance.GetDao<TrackPlayCountRecord>();
            var trackIds = new HashSet<long>(GetLocalLibraryTrackIds());
            var playedTrackIds = new HashSet<long>(trackPlayCountDao.GetPlayedTrackIds());

            var notPlayedTrackIds = trackIds.Except(playedTrackIds);
            var result = new List<ITrackMetadata>();
            foreach (var id in notPlayedTrackIds)
            {
                var metadata = PlaylistDao.GetTrackMetadata(id);
                if (metadata != null)
                {
                    result.Add(metadata);
                }
            }

            return result;
        }
    }
}