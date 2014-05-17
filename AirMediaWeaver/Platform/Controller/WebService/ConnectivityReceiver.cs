
using System;
using AirMedia.Core.Log;
using Android.Content;
using Android.Net;

namespace AirMedia.Platform.Controller.WebService
{
    public class ConnectivityReceiver : BroadcastReceiver
    {
        public static readonly string LogTag = typeof (ConnectivityReceiver).Name;

        public event EventHandler<EventArgs> WifiConnectionEstablished;
        public event EventHandler<EventArgs> WifiConnectionLost;

        public bool IsWifiConnected
        {
            get { return _isWifiConnected; }
        }

        private readonly ConnectivityManager _connectivityManager;
        private bool _isWifiConnected;

        public ConnectivityReceiver(ConnectivityManager connectivityManager)
        {
            _connectivityManager = connectivityManager;
            UpdateWifiInfo();
        }

        public override void OnReceive(Context context, Intent intent)
        {
            bool isFailover = intent.GetBooleanExtra(ConnectivityManager.ExtraIsFailover, false);
            bool hasConnection = intent.GetBooleanExtra(ConnectivityManager.ExtraNoConnectivity, false);

            AmwLog.Debug(LogTag, string.Format("connectivity changed; has connection: {0}",
                intent.HasExtra(ConnectivityManager.ExtraNoConnectivity) ? hasConnection.ToString() : "undefined"));

            if (isFailover)
            {
                AmwLog.Debug(LogTag, "connection is failing over");
            }

            UpdateWifiInfo();
        }

        public void UpdateWifiInfo()
        {
            var networkInfo = _connectivityManager.ActiveNetworkInfo;
            
            bool isWifiConnected = false;

            if (networkInfo != null && networkInfo.Type == ConnectivityType.Wifi)
            {
                isWifiConnected = networkInfo.IsConnected;
            }

            if (_isWifiConnected == isWifiConnected) return;

            _isWifiConnected = isWifiConnected;

            if (_isWifiConnected)
            {
                if (WifiConnectionEstablished != null)
                {
                    WifiConnectionEstablished(this, EventArgs.Empty);
                }
            }
            else
            {
                if (WifiConnectionLost != null)
                {
                    WifiConnectionLost(this, EventArgs.Empty);
                }
            }
        }
    }
};