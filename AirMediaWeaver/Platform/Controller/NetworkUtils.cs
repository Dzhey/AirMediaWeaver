using System;
using AirMedia.Core.Log;
using Android.Content;
using Android.Net.Wifi;
using Java.Net;
using Org.Apache.Http.Conn.Util;


namespace AirMedia.Platform.Controller
{
    public static class NetworkUtils
    {
        private static readonly string LogTag = typeof (NetworkUtils).Name;

        public static readonly string[] DefaultSupportedInterfaces = new[] {"wlan"};

        public static InetAddress GetBroadcastAddress(InetAddress address)
        {
            var ni = NetworkInterface.GetByInetAddress(address);

            if (ni == null) return null;

            var addresses = ni.InterfaceAddresses;
            foreach (var interfaceAddress in addresses)
            {
                return interfaceAddress.Broadcast;
            }

            return null;
        }

        public static InetAddress GetLanIpV4Address(string[] supportedInterfaces = null)
        {
            if (supportedInterfaces == null || supportedInterfaces.Length == 0)
            {
                supportedInterfaces = DefaultSupportedInterfaces;
            }

            var interfaces = NetworkInterface.NetworkInterfaces;
            while (interfaces.HasMoreElements)
            {
                var ni = interfaces.NextElement() as NetworkInterface;

                if (ni == null)
                {
                    AmwLog.Error(LogTag, "Error trying to type cast java network interface");
                    continue;
                }

                bool isSuitable = false;
                foreach (var s in supportedInterfaces)
                {
                    if (ni.Name.Contains(s) && ni.IsUp) isSuitable = true;
                    break;
                }
                if (isSuitable == false) continue;

                var addresses = ni.InetAddresses;
                
                while (addresses.HasMoreElements)
                {
                    var addr = addresses.NextElement() as InetAddress;

                    if (addr == null)
                    {
                        AmwLog.Error(LogTag, addresses.NextElement(), "Error trying to type cast java inet address");
                        continue;
                    }

                    if (addr.IsLoopbackAddress == false && InetAddressUtils.IsIPv4Address(addr.HostAddress))
                    {
                        return addr;
                    }
                }
            }

            return null;
        }

        public static int BytesToIpV4Address(byte[] address)
        {
            return BitConverter.ToInt32(address, 0);
        }

        public static byte[] IpV4ToBytes(int ipAddress)
        {
            return BitConverter.GetBytes(ipAddress);
        }

        public static bool CheckIsWifiHotstopEnabled()
        {
            var wifiManager = (WifiManager) App.Instance.GetSystemService(Context.WifiService);

            try
            {
                var method = wifiManager.Class.GetMethod("isWifiApEnabled");

                if (method != null)
                {
                    var result = method.Invoke(wifiManager, new Java.Lang.Object[0]) as Java.Lang.Boolean;

                    return result != null && (bool) result;
                }
            }
            catch (Exception e)
            {
                AmwLog.Warn(LogTag, "error accessing to Wi-Fi AP status method", e.ToString());
            }

            return false;
        }
    }
}