using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class SimpleRequest<TData> : AbsRequest 
    {
        private readonly Func<TData, RequestResult> _action;

        public string Action { get; private set; }
        public TData InputData { get; private set; }

        public SimpleRequest(TData inputData, Func<TData, RequestResult> func, string action = null)
        {
            _action = func;
            InputData = inputData;
            Action = action;
        }

        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var result = _action(InputData);

            status = result.ResultCode == RequestResult.ResultCodeOk
                         ? RequestStatus.Ok
                         : RequestStatus.Failed;

            return result;
        }
    }
}
