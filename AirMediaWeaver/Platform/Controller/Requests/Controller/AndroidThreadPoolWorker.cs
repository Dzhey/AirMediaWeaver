using System;
using AirMedia.Core.Requests.Controller;

namespace AirMedia.Platform.Controller.Requests.Controller
{
    public class AndroidThreadPoolWorker : ThreadPoolWorker
    {
        public AndroidThreadPoolWorker(int threadPoolSize = MaxDegreeOfParallelism) : base(threadPoolSize)
        {
        }

        protected override void InvokeOnMainThread(Action action)
        {
            App.MainHandler.Post(action);
        }
    }
}