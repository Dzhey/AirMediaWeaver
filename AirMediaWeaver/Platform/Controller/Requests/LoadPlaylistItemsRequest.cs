using System.Collections.Generic;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadPlaylistItemsRequest : BaseLoadRequest<List<TrackMetadata>>
    {
        public long PlaylistId { get; private set; }

        public LoadPlaylistItemsRequest(long playlistId)
        {
            PlaylistId = playlistId;
        }

        protected override LoadRequestResult<List<TrackMetadata>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var metadata = PlaylistDao.GetPlaylistTracks(PlaylistId);

            return new LoadRequestResult<List<TrackMetadata>>(RequestResult.ResultCodeOk, metadata);
        }
    }
}