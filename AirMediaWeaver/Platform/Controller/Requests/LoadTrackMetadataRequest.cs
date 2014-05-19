using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadTrackMetadataRequest : BaseLoadRequest<ITrackMetadata>
    {
        public long? TrackId { get; private set; }
        public string TrackGuid { get; private set; }

        public LoadTrackMetadataRequest(long? trackId, string trackGuid = null)
        {
            TrackId = trackId;
            TrackGuid = trackGuid;
        }

        protected override LoadRequestResult<ITrackMetadata> DoLoad(out RequestStatus status)
        {
            ITrackMetadata result = null;
            
            if (TrackId != null)
            {
                result = MetadataResolver.ResolveMetadata((long) TrackId);
            }
            else
            {
                var pubDao = DatabaseHelper.Instance.TrackMetadataDao;
                result = pubDao.GetTrackMetadata(TrackGuid);
            }

            if (result != null)
            {
                status = RequestStatus.Ok;
            }
            else
            {
                AmwLog.Error(LogTag, string.Format("can't resolve metadata for track id \"{0}\"; " +
                                                   "guid: \"{1}\"", TrackId, TrackGuid));
                status = RequestStatus.Failed;
            }

            return new LoadRequestResult<ITrackMetadata>(RequestResult.ResultCodeOk, result);
        }
    }
}