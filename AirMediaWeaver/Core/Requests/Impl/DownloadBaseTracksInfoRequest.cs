using System.Collections.Generic;
using System.Net;
using AirMedia.Core.Controller;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Controller.WebService.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.WebService.Http;
using AirMedia.Platform.Data;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Requests.Impl
{
    public abstract class DownloadBaseTracksInfoRequest : AbsWebRequest
    {
        public class RequestResult : WebRequestResult
        {
            public TrackMetadata[] TrackInfo { get; set; }

            public RequestResult(int resultCode) : base(resultCode)
            {
            }
        }

        protected override WebRequestResult ExecuteWebRequest(out RequestStatus status, string url)
        {
            var peerInfo = GetAvailablePeersInfo();

            if (peerInfo.Length < 1)
            {
                AmwLog.Debug(LogTag, "no available peers found to perform track info lookup");
                
                status = RequestStatus.Ok;

                return new RequestResult(BaseRequestResult.ResultCodeOk)
                    {
                        TrackInfo = new TrackMetadata[0]
                    };
            }

            var webclient = new AmwWebClient();
            var result = LookupTrackMetadata(webclient, peerInfo);

            status = RequestStatus.Ok;

            return new RequestResult(BaseRequestResult.ResultCodeOk)
                {
                    TrackInfo = result
                };
        }

        protected abstract PeerDescriptor[] GetAvailablePeersInfo();

        private TrackMetadata[] LookupTrackMetadata(WebClient webClient, PeerDescriptor[] peerInfo)
        {
            var metadata = new List<TrackMetadata>();

            foreach (var peer in peerInfo)
            {
                metadata.AddRange(LookupPeerTrackMetadata(webClient, peer));
            }

            return metadata.ToArray();
        }

        private TrackMetadata[] LookupPeerTrackMetadata(WebClient webClient, PeerDescriptor peerInfo)
        {
            if (peerInfo.IpAddress == null)
            {
                AmwLog.Warn(LogTag, string.Format(
                    "can't lookup peer track info: peer ip address unknown; peer: \"{0}\"", peerInfo));

                return new TrackMetadata[0];
            }

            AmwLog.Debug(LogTag, string.Format("downloading base tracks info from peer: {0}", peerInfo));
            string data = webClient.DownloadString(GetTracksUri(peerInfo.IpAddress));

            var model = HttpResponseFactory.UnpackResponse(data);
            if (model == null)
            {
                AmwLog.Error(LogTag, string.Format("can't parse peer response; peer: {0}", peerInfo));
                return new TrackMetadata[0];
            }

            var response = HttpResponseFactory.CreateResponse(model);
            var trackInfoResponse = response as HttpResponsePublishedTracks;
            if (trackInfoResponse == null)
            {
                AmwLog.Error(LogTag, string.Format("obtained invalid response; peer: {0}" +
                                                   "; response: {1}", peerInfo, response));
                return new TrackMetadata[0];
            }
            AmwLog.Debug(LogTag, string.Format("downloaded ({0}) tracks info from " +
                                               "peer: {1}", trackInfoResponse.TrackInfo.Length, peerInfo));

            return HttpContentProvider.CreateTracksMetadata(trackInfoResponse.TrackInfo);
        }

        private string GetTracksUri(string ipAddress)
        {
            return string.Format("http://{0}:{1}/{2}/{3}/", ipAddress, Consts.DefaultHttpPort,
                                 Consts.UriPublicationsFragment, Consts.UriTracksFragment);
        }
    }
}