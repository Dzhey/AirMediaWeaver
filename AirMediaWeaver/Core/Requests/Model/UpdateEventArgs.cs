using AirMedia.Core.Requests.Abs;

namespace AirMedia.Core.Requests.Model
{
    public class UpdateEventArgs : System.EventArgs 
    {
        public AbsRequest Request { get; private set; }
        public UpdateData UpdateData { get; private set; }

        public UpdateEventArgs(AbsRequest request, UpdateData updateData)
        {
            Request = request;
            UpdateData = updateData;
        }
    }
}
