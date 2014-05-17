using System;
using System.Net;
using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.WebService.Http;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;

namespace AirMedia.Platform.Controller.WebService
{
    [Service(Exported = false, Enabled = true)]
    public class AirStreamerService : Service
    {
        private static readonly string LogTag = typeof (AirStreamerService).Name;

        public const string ActionStartHttp = "org.eb.airmedia.android.intent.action.START_HTTP_SERVER";
        public const string ActionStopHttp = "org.eb.airmedia.android.intent.action.STOP_HTTP_SERVER";
        
        private const string WifiLockName = "airmediaweaver_streamer_wifi_lock";
        private const string WifiMulticastLockName = "airmediaweaver_streamer_wifi_multicast_lock";

        private MulticastUdpServer _multicastUdpServer;
        private HttpServer _httpServer;
        private HttpContentProvider _httpContentProvider;
        private WifiManager _wifiManager;
        private PeerManager _peerManager;
        private ConnectivityReceiver _connectivityReceiver;
        private ConnectivityManager _connectivityManager;
        private WifiManager.WifiLock _wifiLock;
        private WifiManager.MulticastLock _multicastLock;
        private IHttpRequestHandler _httpRequestHandler;
        private bool _isStopped;

        public override void OnCreate()
        {
            base.OnCreate();

            _wifiManager = (WifiManager) GetSystemService(WifiService);
            _connectivityManager = (ConnectivityManager) GetSystemService(ConnectivityService);
            _httpContentProvider = new HttpContentProvider();
            _httpRequestHandler = new HttpRequestHandler(_httpContentProvider);
            _httpServer = new HttpServer(_httpRequestHandler);
            _peerManager = new PeerManager(CoreUserPreferences.Instance.ClientGuid);
            _multicastUdpServer = new MulticastUdpServer();
            _multicastUdpServer.AuthPacketReceived += OnAuthPacketReceived;
            _connectivityReceiver = new ConnectivityReceiver(_connectivityManager);

            _wifiLock = _wifiManager.CreateWifiLock(WifiMode.Full, WifiLockName);
            _multicastLock = _wifiManager.CreateMulticastLock(WifiMulticastLockName);

            _wifiLock.Acquire();
            _multicastLock.Acquire();
            AmwLog.Debug(LogTag, "wifi locks acquired");

            _connectivityReceiver.WifiConnectionEstablished += OnWifiConnectionEstablished;
            _connectivityReceiver.WifiConnectionLost += OnWifiConnectionLost;
            var filter = new IntentFilter(ConnectivityManager.ConnectivityAction);
            RegisterReceiver(_connectivityReceiver, filter);

            AmwLog.Debug(LogTag, "AirStreamerService created");
        }

        private void OnWifiConnectionEstablished(object sender, EventArgs args)
        {
            if (_isStopped) return;

            if (_httpServer.IsListening == false)
            {
                AmwLog.Debug(LogTag, "wifi connection established; starting http listener..");
                TryStartHttpServer();
            }

            if (_multicastUdpServer.IsStarted == false)
            {
                AmwLog.Debug(LogTag, "starting multicast udp server..");
                TryStartMulticastUdpServer();
            }
        }

        private void OnAuthPacketReceived(object sender, AuthPacketReceivedEventArgs args)
        {
            _peerManager.UpdatePeers(args.Packet);
        }

        private void OnWifiConnectionLost(object sender, EventArgs args)
        {
            if (_isStopped) return;

            if (_httpServer.IsListening)
            {
                AmwLog.Debug(LogTag, "wifi connection lost; stopping http listener..");
                _httpServer.Stop();
            }

            if (_multicastUdpServer.IsStarted)
            {
                AmwLog.Debug(LogTag, "stopping multicast udp server..");
                TryStartMulticastUdpServer();_multicastUdpServer.Stop();
            }
        }

        private void TryStartHttpServer()
        {
            if (_connectivityReceiver.IsWifiConnected == false)
            {
                AmwLog.Warn(LogTag, "can't start http server: wifi is down");

                return;
            }

            int ipAddress = _wifiManager.ConnectionInfo.IpAddress;
            bool isStarted = _httpServer.TryStart(ipAddress);
            if (!isStarted)
            {
                AmwLog.Error(LogTag, "unable to start http server");
            }
            else
            {
                AmwLog.Info(LogTag, "http server sucessfully started");
            }
        }

        private void TryStartMulticastUdpServer()
        {
            if (_connectivityReceiver.IsWifiConnected == false)
            {
                AmwLog.Warn(LogTag, "can't start udp multicast server: wifi is down");

                return;
            }

            int ipAddress = _wifiManager.ConnectionInfo.IpAddress;
            bool isStarted = _multicastUdpServer.TryStart(ipAddress);
            if (!isStarted)
            {
                AmwLog.Error(LogTag, "unable to start multicast server");
            }
            else
            {
                AmwLog.Info(LogTag, "multicast server sucessfully started");
            }
        }

        public override void OnDestroy()
        {
            _multicastUdpServer.AuthPacketReceived -= OnAuthPacketReceived;

            _connectivityReceiver.WifiConnectionEstablished -= OnWifiConnectionEstablished;
            _connectivityReceiver.WifiConnectionLost -= OnWifiConnectionLost;
            UnregisterReceiver(_connectivityReceiver);

            _wifiLock.Release();
            _multicastLock.Release();
            _httpServer.Dispose();
            _multicastUdpServer.Dispose();

            base.OnDestroy();

            AmwLog.Debug(LogTag, "AirStreamerService destroyed");
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent == null) return StartCommandResult.Sticky;

            switch (intent.Action)
            {
                case ActionStartHttp:
                    _isStopped = false;
                    TryStartHttpServer();
                    TryStartMulticastUdpServer();
                    break;

                case ActionStopHttp:
                    _isStopped = true;
                    _httpServer.Stop();
                    _multicastUdpServer.Stop();
                    break;

                default:
                    AmwLog.Error(LogTag, string.Format("unexpected intent " +
                                                       "action received: \"{0}\"", intent.Action));
                    break;
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }
}