using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class PerformTracksSearchRequest : BaseLoadRequest<List<ITrackMetadata>>
    {
        private class TrackIdEqualityComparer : IEqualityComparer<ITrackMetadata>
        {
            public bool Equals(ITrackMetadata x, ITrackMetadata y)
            {
                return x.TrackId == y.TrackId && x.TrackGuid == y.TrackGuid;
            }

            public int GetHashCode(ITrackMetadata obj)
            {
                return (int)obj.TrackId;
            }
        }

        public TrackSearchCriteria SearchCriteria { get; private set; }
        public string SearchString { get; private set; }

        public PerformTracksSearchRequest(TrackSearchCriteria criteria, string searchString)
        {
            SearchCriteria = criteria;
            SearchString = searchString;
        }

        protected override LoadRequestResult<List<ITrackMetadata>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            AmwLog.Verbose(LogTag, string.Format("performing tracks search; criteria: \"{0}\"", SearchCriteria));
            var tracks = new List<ITrackMetadata>();

            var metadataDao = DatabaseHelper.Instance.TrackMetadataDao;

            var searchWatch = new Stopwatch();
            searchWatch.Start();
            switch (SearchCriteria)
            {
                case TrackSearchCriteria.All:
                    tracks.AddRange(metadataDao.QueryLocalTracksForTitleLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForTitleLike(SearchString));
                    tracks.AddRange(metadataDao.QueryLocalTracksForArtistNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForArtistNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryLocalTracksForAlbumNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForAlbumNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryLocalTracksForGenreNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForGenreNameLike(SearchString));
                    tracks = tracks.Distinct(new TrackIdEqualityComparer()).ToList();
                    break;

                case TrackSearchCriteria.Title:
                    tracks.AddRange(metadataDao.QueryLocalTracksForTitleLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForTitleLike(SearchString));
                    break;

                case TrackSearchCriteria.Artist:
                    tracks.AddRange(metadataDao.QueryLocalTracksForArtistNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForArtistNameLike(SearchString));
                    break;

                case TrackSearchCriteria.Album:
                    tracks.AddRange(metadataDao.QueryLocalTracksForAlbumNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForAlbumNameLike(SearchString));
                    break;

                case TrackSearchCriteria.Genre:
                    tracks.AddRange(metadataDao.QueryLocalTracksForGenreNameLike(SearchString));
                    tracks.AddRange(metadataDao.QueryRemoteTracksForGenreNameLike(SearchString));
                    break;

                default:
                    AmwLog.Error(LogTag, "undefined search criteria requested: \"{0}\"", SearchCriteria);
                    status = RequestStatus.Failed;
                    return new LoadRequestResult<List<ITrackMetadata>>(RequestResult.ResultCodeFailed, null);
            }

            List<ITrackMetadata> result;
            var watch = new Stopwatch();
            watch.Start();
            if (tracks.Count > 1)
            {
                result = tracks.OrderBy(track => track.TrackTitle)
                               .ThenBy(track => track.Artist)
                               .ThenBy(track => track.Album)
                               .ToList();
            }
            else
            {
                result = tracks.ToList();
            }
            watch.Stop();
            searchWatch.Stop();
            AmwLog.Debug(LogTag, "search results sorted in \"{0}\" millis", watch.ElapsedMilliseconds);
            AmwLog.Debug(LogTag, "search results built in \"{0}\" millis", searchWatch.ElapsedMilliseconds);

            return new LoadRequestResult<List<ITrackMetadata>>(RequestResult.ResultCodeOk, result);
        }
    }
}