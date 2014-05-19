using System.Collections.Generic;
using System.Net;
using AirMedia.Core.Controller;
using AirMedia.Core.Controller.WebService.Http;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.WebService.Http;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Requests.Impl
{
    public abstract class DownloadBaseTracksInfoRequest : AbsWebRequest
    {
        public class RequestResult : WebRequestResult
        {
            public RemoteTrackMetadata[] TrackInfo { get; set; }

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
                        TrackInfo = new RemoteTrackMetadata[0]
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

        protected abstract IPeerDescriptor[] GetAvailablePeersInfo();

        private RemoteTrackMetadata[] LookupTrackMetadata(WebClient webClient, IEnumerable<IPeerDescriptor> peerInfo)
        {
            var metadata = new List<RemoteTrackMetadata>();

            foreach (var peer in peerInfo)
            {
                metadata.AddRange(LookupPeerTrackMetadata(webClient, peer));
            }

            return metadata.ToArray();
        }

        private RemoteTrackMetadata[] LookupPeerTrackMetadata(WebClient webClient, IPeerDescriptor peerInfo)
        {
            if (peerInfo.Address == null)
            {
                AmwLog.Warn(LogTag, string.Format(
                    "can't lookup peer track info: peer ip address unknown; peer: \"{0}\"", peerInfo));

                return new RemoteTrackMetadata[0];
            }

            AmwLog.Debug(LogTag, string.Format("downloading base tracks info from peer: {0}", peerInfo));

            try
            {
                string data = webClient.DownloadString(GetTracksUri(peerInfo.Address));

                var model = HttpResponseFactory.UnpackResponse(data);
                if (model == null)
                {
                    AmwLog.Error(LogTag, string.Format("can't parse peer response; peer: {0}", peerInfo));
                    return new RemoteTrackMetadata[0];
                }

                var response = HttpResponseFactory.CreateResponse(model);
                var trackInfoResponse = response as HttpResponsePublishedTracks;
                if (trackInfoResponse == null)
                {
                    AmwLog.Error(LogTag, string.Format("obtained invalid response; peer: {0}" +
                                                       "; response: {1}", peerInfo, response));
                    return new RemoteTrackMetadata[0];
                }

                AmwLog.Debug(LogTag, string.Format("downloaded ({0}) tracks info from " +
                                                   "peer: {1}", trackInfoResponse.TrackInfo.Length, peerInfo));

                return HttpContentProvider.CreateRemoteTracksMetadata(
                    peerInfo.PeerGuid, trackInfoResponse.TrackInfo);
            }
            catch (WebException e)
            {
                AmwLog.Warn(LogTag, string.Format(
                    "unable to download peer track publications: web exception caught; " +
                    "peer: \"{0}\"; message: \"{1}\"", peerInfo, e.Message), e.ToString());
            }

            return new RemoteTrackMetadata[0];
        }

        private string GetTracksUri(string ipAddress)
        {
            return string.Format("http://{0}:{1}/{2}/{3}/", ipAddress, Consts.DefaultHttpPort,
                                 Consts.UriPublicationsFragment, Consts.UriTracksFragment);
        }
    }
}