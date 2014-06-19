using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class PlaySystemPlaylistsRequests : AbsRequest
    {
        public const int ResultCodeErrorNoPlaylistsAvailable = 1000;
        public const int ResultCodeErrorNoTracksAvailable = 2000;

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var playlists = PlaylistDao.GetSystemPlaylists();

            if (playlists.Count < 1)
            {
                AmwLog.Error(LogTag, "No playlists found to start playback");
                status = RequestStatus.Failed;

                return new RequestResult(ResultCodeErrorNoPlaylistsAvailable);
            }

            var trackIds = new List<long>();

            foreach (var playlist in playlists)
            {
                var tracks = PlaylistDao.GetPlaylistTracks(playlist.Id);
                trackIds.AddRange(tracks.Select(metadata => metadata.TrackId));
            }

            if (trackIds.Count < 1)
            {
                AmwLog.Error(LogTag, "All playlists are empty; No available tracks to perform playback");
                status = RequestStatus.Failed;

                return new RequestResult(ResultCodeErrorNoTracksAvailable);
            }

            PlayerControl.Play(trackIds.ToArray());

            status = RequestStatus.Ok;
            AmwLog.Info(LogTag, string.Format(
                "{0} tracks enqueued from {1} playlists", trackIds.Count, playlists.Count));

            return RequestResult.ResultOk;
        }
    }
}