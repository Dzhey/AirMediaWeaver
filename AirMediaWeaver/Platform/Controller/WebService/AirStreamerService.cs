using System;
using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Log;
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

        private HttpServer _httpServer;
        private WifiManager _wifiManager;
        private ConnectivityReceiver _connectivityReceiver;
        private ConnectivityManager _connectivityManager;
        private WifiManager.WifiLock _wifiLock;
        private WifiManager.MulticastLock _multicastLock;
        private bool _isStopped;

        public override void OnCreate()
        {
            base.OnCreate();

            _wifiManager = (WifiManager) GetSystemService(WifiService);
            _connectivityManager = (ConnectivityManager) GetSystemService(ConnectivityService);
            _httpServer = new HttpServer();
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
                _httpServer.TryStart();
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
        }

        public override void OnDestroy()
        {
            _connectivityReceiver.WifiConnectionEstablished -= OnWifiConnectionEstablished;
            _connectivityReceiver.WifiConnectionLost -= OnWifiConnectionLost;
            UnregisterReceiver(_connectivityReceiver);

            _wifiLock.Release();
            _multicastLock.Release();
            _httpServer.Dispose();
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
                    if (_connectivityReceiver.IsWifiConnected)
                    {
                        bool isStarted = _httpServer.TryStart();
                        if (!isStarted)
                        {
                            AmwLog.Error(LogTag, "unable to start http server");
                        }
                    }
                    else
                    {
                        AmwLog.Warn(LogTag, "can't start http server: wifi is down");
                    }
                    break;

                case ActionStopHttp:
                    _isStopped = true;
                    _httpServer.Stop();
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