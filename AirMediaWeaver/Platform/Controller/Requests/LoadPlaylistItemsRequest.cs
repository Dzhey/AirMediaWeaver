using System.Collections.Generic;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadPlaylistItemsRequest : BaseLoadRequest<List<ITrackMetadata>>
    {
        public long PlaylistId { get; private set; }

        public LoadPlaylistItemsRequest(long playlistId)
        {
            PlaylistId = playlistId;
        }

        protected override LoadRequestResult<List<ITrackMetadata>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var metadata = PlaylistDao.GetPlaylistTracks(PlaylistId)
                                      .ConvertAll(input => (ITrackMetadata) input);

            return new LoadRequestResult<List<ITrackMetadata>>(RequestResult.ResultCodeOk, metadata);
        }
    }
}