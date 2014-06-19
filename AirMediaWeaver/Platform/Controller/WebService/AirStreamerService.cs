using System;
using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Impl;
using AirMedia.Platform.Controller.WebService.Http;
using AirMedia.Platform.Logger;
using AirMedia.Platform.Player.MediaService;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Java.Net;

namespace AirMedia.Platform.Controller.WebService
{
    [Service(Exported = false, Enabled = true)]
    public class AirStreamerService : Service, IAmwStreamerService
    {
        private static readonly string LogTag = typeof (AirStreamerService).Name;

        public const string ActionStartHttp = "org.eb.airmedia.android.intent.action.START_HTTP_SERVER";
        public const string ActionStopHttp = "org.eb.airmedia.android.intent.action.STOP_HTTP_SERVER";
        
        private const string WifiLockName = "airmediaweaver_streamer_wifi_lock";
        private const string WifiMulticastLockName = "airmediaweaver_streamer_wifi_multicast_lock";

        private AirStreamerBinder _binder;
        private MulticastUdpServer _multicastUdpServer;
        private HttpServer _httpServer;
        private HttpContentProvider _httpContentProvider;
        private WifiManager _wifiManager;
        private PeerManager _peerManager;
        private ConnectivityReceiver _connectivityReceiver;
        private ConnectivityManager _connectivityManager;
        private WifiManager.WifiLock _wifiLock;
        private WifiManager.MulticastLock _multicastLock;
        private RequestResultListener _requestResultListener;
        private IHttpRequestHandler _httpRequestHandler;
        private bool _isStopped;
        private bool _isWifiHotspotEnabled;

        public override void OnCreate()
        {
            base.OnCreate();

            _wifiManager = (WifiManager) GetSystemService(WifiService);
            _connectivityManager = (ConnectivityManager) GetSystemService(ConnectivityService);
            _httpContentProvider = new HttpContentProvider();
            _httpRequestHandler = new HttpRequestHandler(_httpContentProvider);
            _httpRequestHandler.AuthPacketReceived += OnAuthPacketReceived;
            _httpServer = new HttpServer(_httpRequestHandler);
            _peerManager = new AndroidPeerManager();
            _multicastUdpServer = new MulticastUdpServer();
            _multicastUdpServer.AuthPacketReceived += OnAuthPacketReceived;
            _connectivityReceiver = new ConnectivityReceiver(_connectivityManager);
            _requestResultListener = RequestResultListener.NewInstance(typeof (MediaPlayerService).Name);
            _requestResultListener.RegisterResultHandler(typeof(WatchWifiHotspotRequest), OnWifiHotspotWatchFinished);

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

            if (_isWifiHotspotEnabled)
            {
                AmwLog.Info(LogTag, "stopping Wi-Fi hotspot servers..");
                _isWifiHotspotEnabled = false;
                _httpServer.Stop();
                _multicastUdpServer.Stop();
            }
            RequestManager.Instance.CancelAll(WatchWifiHotspotRequest.ActionTagDefault);

            AmwLog.Debug(LogTag, "wifi connection established; starting servers..");
            if (_httpServer.IsListening == false)
            {
                TryStartHttpServer();
            }

            if (_multicastUdpServer.IsStarted == false)
            {
                TryStartMulticastUdpServer();
            }
        }

        private void OnAuthPacketReceived(object sender, AuthPacketReceivedEventArgs args)
        {
            _peerManager.UpdatePeersAsync(args.Packet);

            if (_isStopped == false)
            {
                if (_multicastUdpServer.IsClientInitialized == false 
                    || _multicastUdpServer.HasMulticastCapability == false)
                {
                    var authPacket = new AuthPacket
                        {
                            Guid = CoreUserPreferences.Instance.ClientGuid,
                            IpAddress = _multicastUdpServer.ServiceAddress.ToString()
                        };

                    var rq = new SendHttpAuthPacketRequest(_httpContentProvider,
                        args.Packet.IpAddress, Core.Consts.DefaultHttpPort, authPacket);

                    _requestResultListener.SubmitRequest(rq, true);
                }
            }
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
                AmwLog.Debug(LogTag, "wifi connection lost; stopping multicast udp server..");
                _multicastUdpServer.Stop();
            }

            // Now watch for Wi-Fi hotspot status
            _requestResultListener.SubmitDedicatedRequest(new WatchWifiHotspotRequest());
        }

        private void TryStartHttpServer()
        {
            if (_httpServer.IsListening)
            {
                AmwLog.Warn(LogTag, "unable to start http server: server is already started");
                return;
            }

            var inetAddress = (InetAddress)null;
            if (_connectivityReceiver.IsWifiConnected == false)
            {
                inetAddress = NetworkUtils.GetLanIpV4Address();
                if (inetAddress == null)
                {
                    AmwLog.Warn(LogTag, "can't start http server: wifi is down, " +
                                        "and no suitable network interface found");
                    LaunchWifiHotspotWatchdog();

                    return;
                }
            }

            int ipAddress = inetAddress == null
                                ? _wifiManager.ConnectionInfo.IpAddress
                                : NetworkUtils.BytesToIpV4Address(inetAddress.GetAddress());
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
            if (_multicastUdpServer.IsStarted)
            {
                AmwLog.Warn(LogTag, "unable to start http server: server is already started");
                return;
            }

            AmwLog.Debug(LogTag, "starting multicast udp server..");
            var inetAddress = (InetAddress) null;
            bool hasWifiConnection = true;
            if (_connectivityReceiver.IsWifiConnected == false)
            {
                hasWifiConnection = false;
                inetAddress = NetworkUtils.GetLanIpV4Address();
                if (inetAddress == null)
                {
                    AmwLog.Warn(LogTag, "can't start udp multicast server: wifi is down, " +
                                        "and no suitable network interface found");

                    LaunchWifiHotspotWatchdog();
                    return;
                }
            }
            
            int ipAddress = inetAddress == null
                                ? _wifiManager.ConnectionInfo.IpAddress
                                : NetworkUtils.BytesToIpV4Address(inetAddress.GetAddress());

            _requestResultListener.SubmitRequest(new SimpleRequest<int>(ipAddress,
                    i =>
                    {
                        bool isStarted = _multicastUdpServer.TryStart(ipAddress, hasWifiConnection);
                        if (!isStarted)
                        {
                            AmwLog.Error(LogTag, "unable to start multicast server");
                        }
                        else
                        {
                            AmwLog.Info(LogTag, "multicast server sucessfully started");
                        }

                        return RequestResult.ResultOk;
                    }));
        }

        public override void OnDestroy()
        {
            _requestResultListener.RemoveResultHandler(typeof(WatchWifiHotspotRequest));

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

        private void LaunchWifiHotspotWatchdog()
        {
            var rq = RequestManager.Instance.FindRequest(WatchWifiHotspotRequest.ActionTagDefault);
            if (rq != null 
                && (rq.Status == RequestStatus.InProgress
                    || rq.Status == RequestStatus.Pending))
            {
                AmwLog.Debug(LogTag, "Wi-Fi watchdog already launched");
                return;
            }

            AmwLog.Info(LogTag, "launching wifi hotspot watchdog..");
            _requestResultListener.SubmitDedicatedRequest(new WatchWifiHotspotRequest());
        }

        private void OnWifiHotspotWatchFinished(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Warn(LogTag, string.Format("Wi-Fi hotspot watch request finished with " +
                                                  "status \"{0}\"", args.Request.Status));
                return;
            }

            AmwLog.Info(LogTag, "Wi-Fi hotspot connectivity detected");
            _isWifiHotspotEnabled = true;
            if (_isStopped == false)
            {
                TryStartHttpServer();
                TryStartMulticastUdpServer();
            }
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
                    AmwLog.Error(LogTag, "unexpected intent action received: \"{0}\"", intent.Action);
                    break;
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            if (_binder == null)
            {
                _binder = new AirStreamerBinder(this);
            }

            return _binder;
        }

        public PeerManager GetPeerManager()
        {
            return _peerManager;
        }
    }
}