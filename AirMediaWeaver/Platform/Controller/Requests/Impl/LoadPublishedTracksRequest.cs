using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data.Sql.Dao;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class LoadPublishedTracksRequest : BaseLoadRequest<List<ITrackMetadata>>
    {
        protected override LoadRequestResult<List<ITrackMetadata>> DoLoad(out RequestStatus status)
        {
            var dao = (TrackPublicationsDao) DatabaseHelper.Instance.GetDao<TrackPublicationRecord>();
            var metadata = dao.QueryForBaseTrackInfo();

            status = RequestStatus.Ok;

            return new LoadRequestResult<List<ITrackMetadata>>(RequestResult.ResultCodeOk, metadata);
        }
    }
}