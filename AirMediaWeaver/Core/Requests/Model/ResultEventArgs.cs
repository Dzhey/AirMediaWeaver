using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Model
{
    public class ResultEventArgs : System.EventArgs 
    {
        public AbsRequest Request { get; private set; }
        public RequestResult Result { get; private set; }

        public ResultEventArgs(AbsRequest request, RequestResult result)
        {
            Request = request;
            Result = result;
        }
    }
}
