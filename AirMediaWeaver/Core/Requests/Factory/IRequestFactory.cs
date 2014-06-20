using System;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;

namespace AirMedia.Core.Requests.Factory
{
    public interface IRequestFactory : IDisposable
    {
        event EventHandler<RequestSubmittedEventArgs> RequestSubmitted;

        IRequestFactory SetManager(RequestManager manager);
        IRequestFactory SetParallel(bool isParallel);
        IRequestFactory SetDedicated(bool isDedicated);
        IRequestFactory SetActionTag(string actionTag);
        IRequestFactory SetDistinct(bool isDistinct);
        AbsRequest Submit(params object[] args);
    }
}