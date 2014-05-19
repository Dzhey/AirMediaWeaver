using System;
using System.Linq;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql.Dao;
using Android.Content;
using Android.Provider;
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
                MediaStore.MediaColumns.Data
            };

        public static HttpBaseTrackInfo[] CreateHttpBaseTracksInfo(params TrackMetadata[] tracksMetadata)
        {
            return tracksMetadata.Select(CreateHttpBaseTrackInfo).ToArray();
        }

        public static HttpBaseTrackInfo CreateHttpBaseTrackInfo(TrackMetadata trackMetadata)
        {
            return new HttpBaseTrackInfo
                {
                    Album = trackMetadata.Album,
                    Artist = trackMetadata.Artist,
                    DurationMillis = trackMetadata.TrackDurationMillis,
                    TrackGuid = trackMetadata.TrackGuid,
                    Title = trackMetadata.TrackTitle
                };
        }

        public static TrackMetadata[] CreateTracksMetadata(string peerGuid, params HttpBaseTrackInfo[] tracksInfo)
        {
            return tracksInfo.Select(info => CreateTrackMetadata(peerGuid, info)).ToArray();
        }

        public static TrackMetadata CreateTrackMetadata(string peerGuid, HttpBaseTrackInfo trackInfo)
        {
            return new TrackMetadata
            {
                Album = trackInfo.Album,
                Artist = trackInfo.Artist,
                TrackDurationMillis = trackInfo.DurationMillis,
                TrackGuid = trackInfo.TrackGuid,
                TrackTitle = trackInfo.Title,
                PeerGuid = peerGuid
            };
        }

        public HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo()
        {
            var dao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();

            var publishedTracks = dao.QueryForHttpBaseTrackInfo()
                                     .Select(CreateHttpBaseTrackInfo)
                                     .ToArray();
            return publishedTracks;
        }

        public IHttpTrackContentDescriptor GetHttpTrackInfo(string trackGuid)
        {
            var publicationsDao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();
            var trackInfo = publicationsDao.QueryLocalPublicationForGuid(trackGuid);

            if (trackInfo == null)
            {
                AmwLog.Error(LogTag, string.Format("unable to retrieve published track metadata for " +
                                                   "track guid: \"{0}\"; Track not found", trackGuid));

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
                    AmwLog.Error(LogTag, string.Format("can't retrieve http track content data; tack " +
                                                       "not found; track-id: \"{0}\"", trackInfo.Value.TrackId));

                    return null;
                }

                result.ContentType = cursor.GetString(0);
                result.ContentLength = cursor.GetLong(1);
                result.FilePath = cursor.GetString(2);
            }

            return result;
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
    }
}