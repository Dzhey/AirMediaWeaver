using System;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data.Sql.Dao;

namespace AirMedia.Platform.Controller.Requests
{
    public class UpdatePublishedTracksRequest : AbsRequest
    {
        private readonly long[] _trackIds;

        /// <summary>
        /// </summary>
        /// <param name="trackIds">local track identifiers to publish</param>
        public UpdatePublishedTracksRequest(params long[] trackIds)
        {
            _trackIds = trackIds;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var dao = (PublicTracksDao) App.DatabaseHelper.GetDao<PublicTrackRecord>();

            dao.UpdatePublishedTracks(_trackIds);

            status = RequestStatus.Ok;
            
            return RequestResult.ResultOk;
        }
    }
}