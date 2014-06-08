using System.Linq;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests
{
    public class PlayPlaylistRequest : AbsRequest
    {
        public const int ResultCodeErrorNoTracksAvailable = 1000;

        public long PlaylistId { get; private set; }
        public int? Position { get; private set; }

        public PlayPlaylistRequest(long playlistId, int? position = null)
        {
            PlaylistId = playlistId;
            Position = position;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var tracks = PlaylistDao.GetPlaylistTracks(PlaylistId);

            if (tracks.Count < 1)
            {
                AmwLog.Error(LogTag, "playlist \"{0}\" is empty; can't start playback", PlaylistId);
                status = RequestStatus.Failed;

                return new RequestResult(ResultCodeErrorNoTracksAvailable);
            }

            var trackIds = tracks.Select(metadata => metadata.TrackId).ToArray();

            int position = 0;
            if (Position != null)
            {
                if (Position < 0 || Position >= trackIds.Length)
                {
                    AmwLog.Error(LogTag, "Requested playback position \"{0}\" is out of " +
                                         "playlist bounds ({1},{2}); using start position.",
                                         Position, 0, trackIds.Length);
                }
                else
                {
                    position = (int) Position;
                }
            }

            PlayerControl.Play(trackIds, position);
            AmwLog.Info(LogTag, string.Format(
                "{0} tracks queued from playlist ({1}), starting from position {2}", 
                trackIds.Length, PlaylistId, position));

            status = RequestStatus.Ok;

            return RequestResult.ResultOk;
        }
    }
}