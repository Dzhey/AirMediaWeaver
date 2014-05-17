

namespace AirMedia.Core.Controller.WebService.Http
{
    public class HttpResponsePublishedTracks : HttpResponse
    {
        public override int ResponseType
        {
            get { return HttpResponseFactory.HttpResponsePublishedTracks; }
        }

        public string FakeData { get; set; }
    }
}