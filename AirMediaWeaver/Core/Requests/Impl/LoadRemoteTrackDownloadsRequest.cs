using System.Collections.Generic;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform;
using AirMedia.Platform.Controller.DownloadManager;
using AirMedia.Platform.Data.Sql;
using AirMedia.Platform.Data.Sql.Dao;
using AirMedia.Platform.Data.Sql.Model;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Requests.Impl
{
    public class LoadRemoteTrackDownloadsRequest : LoadRemoteTracksRequest
    {
        public class RequestResult : LoadRequestResult<List<RemoteTrackMetadata>>
        {
            public ISet<string> DownloadTrackGuids { get; set; }

            public RequestResult(int resultCode, List<RemoteTrackMetadata> resultData)
                : base(resultCode, resultData)
            {
            }
        }

        protected override LoadRequestResult<List<RemoteTrackMetadata>> DoLoad(out RequestStatus status)
        {
            var trackDownloadDao = (TrackDownloadsDao) DatabaseHelper.Instance.GetDao<TrackDownloadRecord>();
            var downloadManager = new AmwDownloadManager(App.Instance, trackDownloadDao);
            var tracks = base.DoLoad(out status);
            var trackGuids = new HashSet<string>();

            foreach (var track in tracks.Data)
            {
                if (downloadManager.IsTrackDownloadPresented(track.TrackGuid))
                {
                    trackGuids.Add(track.TrackGuid);
                }
            }

            return new RequestResult(BaseRequestResult.ResultCodeOk, tracks.Data)
                {
                    DownloadTrackGuids = trackGuids
                };
        }
    }
}