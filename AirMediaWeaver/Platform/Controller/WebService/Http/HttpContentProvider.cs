using System.Linq;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Platform.Data.Sql.Dao;

namespace AirMedia.Platform.Controller.WebService.Http
{
    public class HttpContentProvider : IHttpContentProvider
    {
        public HttpBaseTrackInfo[] GetBaseTrackPublicationsInfo()
        {
            var dao = (PublicTracksDao) DatabaseHelper.Instance.GetDao<PublicTrackRecord>();

            var publishedTracks = dao.QueryForHttpBaseTrackInfo()
                                     .Select(input => new HttpBaseTrackInfo
                                         {
                                             Album = input.Album,
                                             Artist = input.ArtistName,
                                             DurationMillis = input.Duration,
                                             PublicGuid = input.TrackGuid,
                                             Title = input.TrackTitle
                                         })
                                     .ToArray();
            return publishedTracks;

        }
    }
}