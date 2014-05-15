using System.Collections.Generic;
using AirMedia.Core.Data;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using Android.OS;

namespace AirMedia.Platform.Controller.Requests
{
    public class LoadPlaylistsRequest : BaseLoadRequest<List<PlaylistModel>>
    {
        public Bundle Payload { get; set; }

        protected override LoadRequestResult<List<PlaylistModel>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var playlists = PlaylistDao.GetSystemPlaylists();

            return new LoadRequestResult<List<PlaylistModel>>(RequestResult.ResultCodeOk, playlists);
        }
    }
}