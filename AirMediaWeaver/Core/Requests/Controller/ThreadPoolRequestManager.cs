using System;
using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Controller
{
    public class ThreadPoolRequestManager : RequestManager
    {
        private ThreadPoolWorker _worker;

        public ThreadPoolRequestManager(int threadPoolSize = ThreadPoolWorker.MaxDegreeOfParallelism)
        {
            _worker = new ThreadPoolWorker(threadPoolSize);
        }

        protected override void SubmitRequestImpl(AbsRequest request, int requestId, bool isParallel, bool isDedicated)
        {
            if (isParallel == false)
                throw new ArgumentException("serial requests is not supported", "isParallel");

            if (isDedicated)
            {
                _worker.SubmitDedicatedRequest(request);
            }
            else
            {
                _worker.SubmitRequest(request);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _worker.Dispose();
                _worker = null;
            }
        }
    }
}