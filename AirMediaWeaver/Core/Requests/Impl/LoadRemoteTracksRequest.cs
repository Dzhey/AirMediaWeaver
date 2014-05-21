using System.Collections.Generic;
using System.Linq;
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

            var remoteTracks = pubDao.GetRemoteTracksMetadata()
                                     .Select(metadata => new RemoteTrackMetadata(metadata))
                                     .ToList();

            return new LoadRequestResult<List<RemoteTrackMetadata>>(
                RequestResult.ResultCodeOk, remoteTracks);
        }
    }
}