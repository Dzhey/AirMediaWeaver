using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class PerformTracksSearchRequest : BaseLoadRequest<List<ITrackMetadata>>
    {
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

            var tracks = DatabaseHelper.Instance
                                       .TrackMetadataDao
                                       .QueryLocalForArtistNameLike(SearchString)
                                       .ToList();

            return new LoadRequestResult<List<ITrackMetadata>>(RequestResult.ResultCodeOk, tracks);
        }
    }
}