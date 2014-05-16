using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql.Dao;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadPublishedTracksRequest : BaseLoadRequest<List<TrackMetadata>>
    {
        protected override LoadRequestResult<List<TrackMetadata>> DoLoad(out RequestStatus status)
        {
            var dao = (PublicTracksDao) DatabaseHelper.Instance.GetDao<PublicTrackRecord>();
            var metadata = dao.QueryForBaseTrackInfo();

            status = RequestStatus.Ok;

            return new LoadRequestResult<List<TrackMetadata>>(RequestResult.ResultCodeOk, metadata);
        }
    }
}