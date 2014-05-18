using System.Net;


namespace AirMedia.Core.Controller
{
    public class AmwWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(System.Uri address)
        {
            var request = base.GetWebRequest(address);

            request.Timeout = Consts.DefaultWebClientTimeout;

            return request;
        }
    }
}