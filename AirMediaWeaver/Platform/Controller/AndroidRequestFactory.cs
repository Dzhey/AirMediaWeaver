using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Factory;
using AirMedia.Platform.Logger;

namespace AirMedia.Platform.Controller
{
    public class AndroidRequestFactory : RequestFactory
    {
        private readonly RequestResultListener _listener;

        public static AndroidRequestFactory Init(string requestTypeName, RequestResultListener listener)
        {
            return new AndroidRequestFactory(requestTypeName, listener);
        }

        public static AndroidRequestFactory Init(Type requestType, RequestResultListener listener)
        {
            return new AndroidRequestFactory(requestType, listener);
        }

        protected AndroidRequestFactory(string requestTypeName, RequestResultListener listener)
            : base(requestTypeName)
        {
            _listener = listener;
        }

        protected AndroidRequestFactory(Type requestType, RequestResultListener listener)
            : base(requestType)
        {
            _listener = listener;
        }


        public override AbsRequest Submit(params object[] args)
        {
            var rq = base.Submit(args);

            if (_listener != null && rq.HasRequestId)
            {
                _listener.AddPendingRequest(rq.RequestId);
            }

            return rq;
        }
    }
}