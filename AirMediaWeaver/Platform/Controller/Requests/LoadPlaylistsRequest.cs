using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadPlaylistsRequest : BaseLoadRequest<List<PlaylistModel>>
    {
        protected override LoadRequestResult<List<PlaylistModel>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var playlists = PlaylistManager.GetSystemPlaylists();

            return new LoadRequestResult<List<PlaylistModel>>(RequestResult.ResultCodeOk, playlists);
        }
    }
}