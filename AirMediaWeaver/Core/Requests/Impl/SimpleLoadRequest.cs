using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class SimpleLoadRequest<T> : BaseLoadRequest<T>
    {
        private readonly Func<T> _action;

        public string Action { get; private set; }

        public SimpleLoadRequest(Func<T> func, string action = null)
        {
            _action = func;
            Action = action;
        }

        protected override LoadRequestResult<T> DoLoad(out RequestStatus status)
        {
            var result = _action();

            status = RequestStatus.Ok;

            return new LoadRequestResult<T>(RequestResult.ResultCodeOk, result);
        }
    }
}
