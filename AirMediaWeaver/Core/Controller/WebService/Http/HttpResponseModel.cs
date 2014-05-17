namespace AirMedia.Core.Controller.WebService.Http
{
    public class HttpResponseModel
    {
        public int ResponseType { get; set; }
        public int ErrorCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string ResponseData { get; set; }
    }
}