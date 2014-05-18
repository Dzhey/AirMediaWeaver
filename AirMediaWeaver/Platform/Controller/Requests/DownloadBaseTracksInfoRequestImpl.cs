using System.Linq;
using System.Threading;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Platform.Controller.WebService;
using Android.Content;

namespace AirMedia.Platform.Controller.Requests
{
    public class DownloadBaseTracksInfoRequestImpl : DownloadBaseTracksInfoRequest
    {
        public const int ServiceConnectionTimeout = 7000;

        private AirStreamerServiceConnection _connection;
        private IAmwStreamerService _service;
        private AutoResetEvent _wait;
        private bool _isDisposed;

        protected override PeerDescriptor[] GetAvailablePeersInfo()
        {
            _connection = new AirStreamerServiceConnection();
            _connection.Connected += OnServiceConnected;
            _wait = new AutoResetEvent(false);

            using (_connection)
            {
                using (_wait)
                {
                    var intent = new Intent(App.Instance, typeof(AirStreamerService));

                    bool ret = App.Instance.BindService(intent, _connection, Bind.AutoCreate);

                    if (!ret)
                    {
                        AmwLog.Error(LogTag, "unable to bind air streamer service");
                        return new PeerDescriptor[0];
                    }

                    if (_wait.WaitOne(ServiceConnectionTimeout) == false)
                    {
                        AmwLog.Error(LogTag, "unable to bind air streamer service: connection timeout");
                        return new PeerDescriptor[0];
                    }

                    AmwLog.Verbose(LogTag, "air streamer service is bound");

                    var peerInfo = _service.GetPeerManager().GetPeers().ToArray();

                    UnbindService();

                    return peerInfo;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                UnbindService();
            }

            base.Dispose(disposing);

            _isDisposed = true;
        }

        private void OnServiceConnected(object sender, IAmwStreamerService service)
        {
            _service = service;
            _wait.Set();
        }

        private void UnbindService()
        {
            if (_connection != null && _connection.IsConnected)
            {
                App.Instance.UnbindService(_connection);
                _connection.Connected -= OnServiceConnected;
            }
        }
    }
}