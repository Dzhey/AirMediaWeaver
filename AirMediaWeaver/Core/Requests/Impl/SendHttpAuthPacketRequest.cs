using System.Globalization;
using AirMedia.Core.Controller;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using Newtonsoft.Json;

namespace AirMedia.Core.Requests.Impl
{
    public class SendHttpAuthPacketRequest : AbsWebRequest
    {
        public string DestinationIpAddress { get; private set; }
        public int DestinationPort { get; private set; }

        private readonly IHttpContentProvider _provider;
        private readonly AuthPacket _packet;

        public SendHttpAuthPacketRequest(IHttpContentProvider contentProvider, 
            string dstIpAddress, int dstPort, AuthPacket packet)
        {
            DestinationIpAddress = dstIpAddress;
            DestinationPort = dstPort;
            _provider = contentProvider;
            _packet = packet;
        }

        protected override WebRequestResult ExecuteWebRequest(out RequestStatus status, string url)
        {
            var webclient = new AmwWebClient();

            var uri = _provider.CreatePutAuthPacketUri(DestinationIpAddress, 
                DestinationPort.ToString(CultureInfo.InvariantCulture));
            string json = JsonConvert.SerializeObject(_packet, Formatting.None);
            AmwLog.Verbose(LogTag, string.Format("sending http auth packet \"{0}\"; " +
                                                 "length: {1}", json, json.Length));

            webclient.UploadString(uri, json);

            status = RequestStatus.Ok;
            return new WebRequestResult(RequestResult.ResultCodeOk);
        }
    }
}