using System.Linq;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class PlayAudioLibraryRequest : AbsRequest
    {
        public const int ResultCodeErrorNoAvailableTracks = 1000;

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var tracks = PlaylistDao.GetSystemTracks();

            var trackIds = tracks.Select(metadata => metadata.TrackId).ToArray();

            if (trackIds.Length > 0)
            {
                PlayerControl.Play(trackIds);
                AmwLog.Info(LogTag, string.Format("{0} tracks enqueued from audio library", trackIds.Length));

                return RequestResult.ResultOk;
            }
            
            AmwLog.Error(LogTag, "audio library is empty; can't start audio library playback");
            status = RequestStatus.Failed;

            return new RequestResult(ResultCodeErrorNoAvailableTracks);
        }
    }
}