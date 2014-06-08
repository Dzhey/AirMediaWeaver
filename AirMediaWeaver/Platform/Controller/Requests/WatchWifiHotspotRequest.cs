using System.Threading;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests
{
    public class WatchWifiHotspotRequest : AbsRequest
    {
        public const int WatchWifiHotspotIntervalMillis = 8000;

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            AmwLog.Debug(LogTag, "watching for Wi-Fi hotspot status..");
            while (true)
            {
                Thread.Sleep(8000);

                if (NetworkUtils.CheckIsWifiHotstopEnabled())
                {
                    status = RequestStatus.Ok;

                    AmwLog.Info(LogTag, "Wi-Fi hotspot detected!");

                    return RequestResult.ResultOk;
                }
            }
        }
    }
}