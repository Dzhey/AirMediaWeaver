using System;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Controller
{
    public interface IWorker
    {
        event EventHandler<ExecutionFinishedEventArgs> ExecutionFinished;

        void SubmitRequest(AbsRequest request);
        bool HasPendingRequests();
    }
}