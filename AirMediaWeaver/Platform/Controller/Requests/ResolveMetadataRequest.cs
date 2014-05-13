using System;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests
{
    public class ResolveMetadataRequest : AbsRequest
    {
        public long TrackId { get; private set; }

        public ResolveMetadataRequest(long trackId)
        {
            TrackId = trackId;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var result = MetadataResolver.ResolveMetadata(TrackId);

            if (result != null)
            {
                status = RequestStatus.Ok;
            }
            else
            {
                AmwLog.Error(LogTag, string.Format("can't resolve metadata for track id \"{0}\"", TrackId));
                status = RequestStatus.Failed;
            }

            return new LoadRequestResult<TrackMetadata?>(RequestResult.ResultCodeOk, result);
        }
    }
}