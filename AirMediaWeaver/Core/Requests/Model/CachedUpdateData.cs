using AirMedia.Core.Requests.Interfaces;

namespace AirMedia.Core.Requests.Model
{
    public class CachedUpdateData : UpdateData
    {
        public ICachedRequestResult CachedResult { get; set; }

        public CachedUpdateData() : base(UpdateData.UpdateCodeCachedResultRetrieved)
        {
        }
    }
}