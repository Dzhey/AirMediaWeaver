using System;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Factory
{
    public class RequestSubmittedEventArgs : EventArgs
    {
        public AbsRequest Request { get; set; }

        public RequestSubmittedEventArgs()
        {
        }

        public RequestSubmittedEventArgs(AbsRequest request)
        {
            Request = request;
        }
    }
}