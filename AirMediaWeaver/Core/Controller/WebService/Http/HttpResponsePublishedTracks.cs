

using AirMedia.Core.Controller.WebService.Model;

namespace AirMedia.Core.Controller.WebService.Http
{
    public class HttpResponsePublishedTracks : HttpResponse
    {
        public override int ResponseType
        {
            get { return HttpResponseFactory.HttpResponsePublishedTracks; }
        }

        public HttpBaseTrackInfo[] TrackInfo { get; set; }
    }
}