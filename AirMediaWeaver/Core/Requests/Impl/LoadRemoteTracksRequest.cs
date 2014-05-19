using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data.Sql;

namespace AirMedia.Core.Requests.Impl
{
    public class LoadRemoteTracksRequest : BaseLoadRequest<List<RemoteTrackMetadata>>
    {
        protected override LoadRequestResult<List<RemoteTrackMetadata>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var pubDao = DatabaseHelper.Instance.TrackMetadataDao;

            var remoteTracks = TrackMetadataDao.CreateRemoteTracksMetadata(
                pubDao.GetRemoteTracksMetadata()).ToList();

            return new LoadRequestResult<List<RemoteTrackMetadata>>(
                RequestResult.ResultCodeOk, remoteTracks);
        }
    }
}