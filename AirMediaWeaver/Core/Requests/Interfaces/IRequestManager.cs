using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Interfaces
{
    public interface IRequestManager
    {
        void RegisterEventHandler(IRequestResultListener handler);
        void RegisterEventHandler(IRequestUpdateListener handler);
        void RemoveEventHandler(IRequestResultListener handler);
        void RemoveEventHandler(IRequestUpdateListener handler);

        AbsRequest FindRequest(int requestId);

        /// <summary>
        /// Submit request on execution.
        /// Request execution order depend on the supplied parameters.
        /// By default, all submitted requests are executed in serial order.
        /// Appropriate parameters chagning execution order.
        /// </summary>
        /// <param name="request">request to enqueue</param>
        /// <param name="isParallel">
        /// If true then request will be executed in the fixed thread pool instead of common single thread. 
        /// Requests with the same tag are executing in serial order in the same thread pool.
        /// Requests with the null tag are executing asynchronously.
        /// </param>
        /// <param name="isDedicated">
        /// If true then request will be asynchronously executed in the dedicated thread.
        /// Request tag and parallel flag are not considered in that case.
        /// </param>
        /// <returns>generated request id</returns>
        int SubmitRequest(AbsRequest request, bool isParallel = false, bool isDedicated = false);
    }
}
