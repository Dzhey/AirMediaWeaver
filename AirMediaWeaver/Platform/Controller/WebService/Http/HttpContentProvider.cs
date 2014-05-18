using System.Linq;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql.Dao;

namespace AirMedia.Platform.Controller.WebService.Http
{
    public class HttpContentProvider : IHttpContentProvider
    {
        public static HttpBaseTrackInfo[] CreateHttpBaseTracksInfo(params TrackMetadata[] tracksMetadata)
        {
            return tracksMetadata.Select(CreateHttpBaseTrackInfo).ToArray();
        }

        public static HttpBaseTrackInfo CreateHttpBaseTrackInfo(TrackMetadata trackMetadata)
        {
            return new HttpBaseTrackInfo
                {
                    Album = trackMetadata.Album,
                    Artist = trackMetadata.ArtistName,
                    DurationMillis = trackMetadata.Duration,
                    PublicGuid = trackMetadata.TrackGuid,
                    Title = trackMetadata.TrackTitle
                };
        }

        public static TrackMetadata[] CreateTracksMetadata(params HttpBaseTrackInfo[] tracksInfo)
        {
            return tracksInfo.Select(CreateTrackMetadata).ToArray();
        }

        public static TrackMetadata CreateTrackMetadata(HttpBaseTrackInfo trackInfo)
        {
            return new TrackMetadata
            {
                Album = trackInfo.Album,
                ArtistName = trackInfo.Artist,
                Duration = trackInfo.DurationMillis,
                TrackGuid = trackInfo.PublicGuid,
                TrackTitle = trackInfo.Title
            };
        }

        public HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo()
        {
            var dao = (PublicTracksDao) DatabaseHelper.Instance.GetDao<PublicTrackRecord>();

            var publishedTracks = dao.QueryForHttpBaseTrackInfo()
                                     .Select(CreateHttpBaseTrackInfo)
                                     .ToArray();
            return publishedTracks;

        }
    }
}