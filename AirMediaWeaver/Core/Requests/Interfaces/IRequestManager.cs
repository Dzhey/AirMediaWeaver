using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Interfaces
{
    interface IRequestManager
    {
        int SubmitRequest(AbsRequest request, bool isParallel = false);
    }
}
