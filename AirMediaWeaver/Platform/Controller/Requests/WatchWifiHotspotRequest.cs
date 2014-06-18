using System.Threading;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Platform.Controller.Requests
{
    public class WatchWifiHotspotRequest : AbsRequest
    {
        public const int WatchWifiHotspotIntervalMillis = 8000;
        public const string ActionTagDefault = "WatchWifiHotspotRequest_Tag";

        public WatchWifiHotspotRequest()
        {
            ActionTag = ActionTagDefault;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            AmwLog.Info(LogTag, "watching for Wi-Fi hotspot status..");
            while (true)
            {
                if (IsCancelled)
                {
                    AmwLog.Debug(LogTag, "finishing cancelled Wi-Fi hotspot watch request..");
                    status = RequestStatus.Cancelled;
                    return RequestResult.ResultCancelled;
                }

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