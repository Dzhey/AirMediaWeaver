using AirMedia.Core.Controller.WebService;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using Newtonsoft.Json;

namespace AirMedia.Core.Requests.Impl
{
    public class ReceiveMulticastAuthRequest : AbsRequest
    {
        private readonly IMulticastReceiver _receiver;

        public ReceiveMulticastAuthRequest(IMulticastReceiver receiver)
        {
            _receiver = receiver;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Failed;

            var buffer = new byte[1024];

            if (_receiver.IsStarted == false)
            {
                AmwLog.Error(LogTag, "can't receive auth request: receiver is not active");

                return RequestResult.ResultFailed;
            }

            int length = _receiver.Receive(buffer);

            string json = System.Text.Encoding.UTF8.GetString(buffer, 0, length);

            AmwLog.Verbose(LogTag, string.Format("received udp message: \"{0}\"", json));

            var authPacket = JsonConvert.DeserializeObject(json, typeof (AuthPacket)) as AuthPacket;
            
            if (authPacket == null)
            {
                AmwLog.Error(LogTag, "Can't decode received message");

                return RequestResult.ResultFailed;
            }

            status = RequestStatus.Ok;

            AmwLog.Verbose(LogTag, "FINISHING receive multicast udp message request");

            return new LoadRequestResult<AuthPacket>(RequestResult.ResultCodeOk, authPacket);
        }
    }
}