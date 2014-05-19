using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests;

namespace AirMedia.Platform.Controller.WebService
{
    public class AndroidPeerManager : PeerManager
    {
        private const string UpdateRequestTag = "AndroidPeerManager_update_publications";
        private bool _isDisposed;

        public AndroidPeerManager() : 
            base(CoreUserPreferences.Instance.ClientGuid, 
            (PeersDao) DatabaseHelper.Instance.GetDao<PeerRecord>())
        {
            RequestListener.RegisterResultHandler(typeof(UpdateRemoteTrackPublicationsRequestImpl), 
                OnPublicationsUpdateFinihsed);
        }

        protected override void OnNewPeerDiscovered(PeerRecord peer)
        {
            base.OnNewPeerDiscovered(peer);

            AmwLog.Debug(LogTag, "requesting track publications update");
            var request = new UpdateRemoteTrackPublicationsRequestImpl
                {
                    ActionTag = UpdateRequestTag
                };
            RequestListener.SubmitRequest(request, true);
        }

        protected override void OnPeerUpdated(PeerRecord peer)
        {
            base.OnPeerUpdated(peer);

            AmwLog.Debug(LogTag, "on update peer: requesting track publications update");
            var request = new UpdateRemoteTrackPublicationsRequestImpl
            {
                ActionTag = UpdateRequestTag
            };
            RequestListener.SubmitRequest(request, true);
        }

        protected virtual void OnPublicationsUpdateFinihsed(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, "error updating remote track publications");
                return;
            }

            AmwLog.Info(LogTag, "track publications sucessfully updated");
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                RequestListener.RemoveResultHandler(typeof(UpdateRemoteTrackPublicationsRequestImpl));
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }
    }
}