using System;
using System.IO;
using System.Linq;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql;
using AirMedia.Platform.Data.Sql.Dao;
using Android.Content;
using Android.Provider;
using Android.Webkit;
using Consts = AirMedia.Core.Consts;

namespace AirMedia.Platform.Controller.WebService.Http
{
    public class HttpContentProvider : IHttpContentProvider
    {
        public static readonly string LogTag = typeof(HttpContentProvider).Name;

        private static readonly string[] QueryHttpTrackDataProjection = new[]
            {
                MediaStore.MediaColumns.MimeType,
                MediaStore.MediaColumns.Size,
                MediaStore.MediaColumns.Data,
                MediaStore.MediaColumns.DisplayName
            };

        public static HttpBaseTrackInfo[] CreateHttpBaseTracksInfo(params IRemoteTrackMetadata[] tracksMetadata)
        {
            return tracksMetadata.Select(CreateHttpBaseTrackInfo).ToArray();
        }

        public static HttpBaseTrackInfo CreateHttpBaseTrackInfo(IRemoteTrackMetadata trackMetadata)
        {
            return new HttpBaseTrackInfo
                {
                    Album = trackMetadata.Album,
                    Artist = trackMetadata.Artist,
                    DurationMillis = trackMetadata.TrackDurationMillis,
                    TrackGuid = trackMetadata.TrackGuid,
                    Title = trackMetadata.TrackTitle,
                    ContentType = trackMetadata.ContentType,
                    Genre = trackMetadata.Genre
                };
        }

        public static RemoteTrackMetadata[] CreateRemoteTracksMetadata(string peerGuid, params HttpBaseTrackInfo[] tracksInfo)
        {
            return tracksInfo.Select(info => CreateRemoteTrackMetadata(peerGuid, info)).ToArray();
        }

        public static RemoteTrackMetadata CreateRemoteTrackMetadata(string peerGuid, HttpBaseTrackInfo trackInfo)
        {
            return new RemoteTrackMetadata
            {
                Album = trackInfo.Album,
                Artist = trackInfo.Artist,
                TrackDurationMillis = trackInfo.DurationMillis,
                TrackGuid = trackInfo.TrackGuid,
                ContentType = trackInfo.ContentType,
                TrackTitle = trackInfo.Title,
                PeerGuid = peerGuid,
                Genre = trackInfo.Genre
            };
        }

        public HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo()
        {
            var dao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();

            var publishedTracks = dao.QueryForHttpBaseTrackInfo().ToArray();
            var result = new HttpBaseTrackInfo[publishedTracks.Length];

            int i = 0;
            foreach (var track in publishedTracks)
            {
                result[i] = CreateHttpBaseTrackInfo(track);
                i++;
            }
            return result;
        }

        public IHttpTrackContentDescriptor GetHttpTrackInfo(string trackGuid)
        {
            var publicationsDao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();
            var trackInfo = publicationsDao.QueryLocalPublicationForGuid(trackGuid);

            if (trackInfo == null)
            {
                AmwLog.Error(LogTag, "unable to retrieve published track metadata " +
                                     "for track guid: \"{0}\"; Track not found", trackGuid);

                return null;
            }

            var result = new HttpTrackContentDescriptor();
            var uri = ContentUris.WithAppendedId(MediaStore.Audio.Media.ExternalContentUri, trackInfo.Value.TrackId);
            var resolver = App.Instance.ContentResolver;
            var cursor = resolver.Query(uri, QueryHttpTrackDataProjection, null, null, null);

            using (cursor)
            {
                if (cursor.MoveToFirst() == false)
                {
                    AmwLog.Error(LogTag, "can't retrieve http track content data; " +
                                         "tack not found; track-id: \"{0}\"", trackInfo.Value.TrackId);

                    return null;
                }

                result.ContentType = cursor.GetString(0);
                result.ContentLength = cursor.GetLong(1);
                result.FilePath = cursor.GetString(2);
                result.FileName = cursor.GetString(3);
                
                if (string.IsNullOrWhiteSpace(result.FileName))
                {
                    try
                    {
                        AmwLog.Debug(LogTag, "can't resolve display name for track; using content uri", trackGuid);
                        result.FileName = Path.GetFileName(result.FilePath);
                    }
                    catch (ArgumentException e)
                    {
                        AmwLog.Warn(LogTag, "can't resolve track file name from content uri", e.ToString());
                        result.FileName = trackGuid;
                    }
                }
            }

            return result;
        }

        public Uri CreatePutAuthPacketUri(string address, string port)
        {
            return new Uri("{scheme}://{address}:{port}/{content}/{update}/"
                               .HaackFormat(new
                               {
                                   scheme = "http",
                                   address,
                                   port,
                                   content = Consts.UriPeersFragment,
                                   update = Consts.UriPeersUpdateFragment
                               }));
        }

        public Uri CreateRemoteTrackUri(string address, string port, string trackGuid)
        {
            return new Uri("{scheme}://{address}:{port}/{content}/{tracks}/{trackId}"
                               .HaackFormat(new
                               {
                                   scheme = "http",
                                   address,
                                   port,
                                   content = Consts.UriPublicationsFragment,
                                   tracks = Consts.UriTracksFragment,
                                   trackId = trackGuid
                               }));
        }

        public Uri CreateTrackDownloadDestinationUri(IRemoteTrackMetadata metadata)
        {
            string path = "file://" + Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath,
                                                   Android.OS.Environment.DirectoryMusic);

            // TODO: create folders if needed
//            if (string.IsNullOrWhiteSpace(metadata.Artist) == false)
//            {
//                path += "/" + metadata.Artist;
//            }
//
//            if (string.IsNullOrWhiteSpace(metadata.Album) == false)
//            {
//                path += "/" + metadata.Album;
//            }

            if (string.IsNullOrWhiteSpace(metadata.TrackTitle) == false)
            {
                path += "/" + metadata.TrackTitle;
            }
            else
            {
                path += "/" + metadata.TrackGuid;
            }

            string extension = MimeTypeMap.Singleton.GetExtensionFromMimeType(metadata.ContentType);
            if (extension != null)
            {
                path += "." + extension;
            }

            return new Uri(path);
        }
    }
}