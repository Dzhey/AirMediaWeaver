using System;
using System.Collections.Generic;
using System.Reflection;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class BatchRequest : AbsRequest
    {
        public struct BatchRequestArgs
        {
            public ConstructorInfo RequestConstructorInfo { get; set; }
            public object[] RequestConstructorArgs { get; set; }
        }

        public IReadOnlyCollection<BatchRequestArgs> RequestArgs
        {
            get
            {
                if (_readOnlyArgs == null)
                    _readOnlyArgs = Array.AsReadOnly(Args);

                return _readOnlyArgs;
            }
        }

        private IReadOnlyCollection<BatchRequestArgs> _readOnlyArgs;

        protected BatchRequestArgs[] Args { get; private set; }

        public BatchRequest(params BatchRequestArgs[] args)
        {
            Args = args ?? new BatchRequestArgs[0];
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Ok;
            var results = new KeyValuePair<AbsRequest, RequestResult>[Args.Length];

            int counter = 0;
            foreach (var parms in Args)
            {
                var request = (AbsRequest)parms.RequestConstructorInfo.Invoke(parms.RequestConstructorArgs);
                var result = request.Execute();

                OnRequestFinished(request, result);

                results[counter] = new KeyValuePair<AbsRequest, RequestResult>(request, result);
                counter++;
            }

            return new BatchRequestResult(RequestResult.ResultCodeOk)
                {
                    Results = results
                };
        }

        protected virtual void OnRequestFinished(AbsRequest request, RequestResult result)
        {
        }
    }
}