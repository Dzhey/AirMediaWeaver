using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using Newtonsoft.Json;

namespace AirMedia.Core.Requests.Impl
{
    public class SendMulticastAuthRequest : AbsRequest
    {
        private readonly IMulticastSender _sender;
        private readonly string _ipAddress;

        public SendMulticastAuthRequest(IMulticastSender sender, string ipAddress)
        {
            _sender = sender;
            _ipAddress = ipAddress;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var packet = new AuthPacket
                {
                    Guid = CoreUserPreferences.Instance.ClientGuid,
                    IpAddress = _ipAddress
                };

            string json = JsonConvert.SerializeObject(packet, Formatting.None);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            AmwLog.Verbose(LogTag, string.Format("sending multicast auth packet \"{0}\"; " +
                                                 "length: {1}", json, data.Length));

            if (_sender.IsStarted == false)
            {
                AmwLog.Error(LogTag, "can't send multicast auth packet: multicast sender is not active");

                status = RequestStatus.Failed;

                return RequestResult.ResultFailed;
            }

            int ret = _sender.SendMulticast(data, 0, data.Length);

            if (ret != data.Length)
            {
                AmwLog.Error(LogTag, "Error sending multicast packet: " +
                                     "inconsistent sent data length ({0},{1})", data.Length, ret);

                status = RequestStatus.Failed;

                return RequestResult.ResultFailed;
            }

            status = RequestStatus.Ok;

            return RequestResult.ResultOk;
        }
    }
}