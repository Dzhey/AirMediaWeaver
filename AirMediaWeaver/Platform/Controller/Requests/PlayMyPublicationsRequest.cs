using System.Linq;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data.Sql.Dao;
using AirMedia.Platform.Player;

namespace AirMedia.Platform.Controller.Requests
{
    public class PlayMyPublicationsRequest : AbsRequest
    {
        public const int ResultCodeErrorNoTracksAvailable = 1000;

        public int? Position { get; private set; }

        public PlayMyPublicationsRequest(int? position = null)
        {
            Position = position;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var dao = (TrackPublicationsDao) App.DatabaseHelper.GetDao<TrackPublicationRecord>();

            var tracks = dao.QueryForBaseTrackInfo().Select(track => track.TrackId).ToArray();

            if (tracks.Length < 1)
            {
                AmwLog.Warn(LogTag, string.Format("no published tracks found; can't start playback"));
                status = RequestStatus.Failed;

                return new RequestResult(ResultCodeErrorNoTracksAvailable);
            }

            int position = 0;
            if (Position != null)
            {
                if (Position < 0 || Position >= tracks.Length)
                {
                    AmwLog.Error(LogTag, "Requested playback position \"{0}\" is out of " +
                        "publish track list bounds ({1},{2}); using start position.",
                        Position, 0, tracks.Length);
                }
                else
                {
                    position = (int) Position;
                }
            }

            PlayerControl.Play(tracks, position);
            AmwLog.Info(LogTag, string.Format("{0} tracks queued from publish track list, " +
                                              "starting from position {1}", tracks.Length, position));

            status = RequestStatus.Ok;

            return RequestResult.ResultOk;
        }
    }
}