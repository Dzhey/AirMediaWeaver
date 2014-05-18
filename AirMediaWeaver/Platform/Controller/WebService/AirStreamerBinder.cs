
using Android.OS;

namespace AirMedia.Platform.Controller.WebService
{
    public class AirStreamerBinder : Binder
    {
        public IAmwStreamerService Service { get { return _service; }}

        private readonly IAmwStreamerService _service;

        public AirStreamerBinder(IAmwStreamerService service)
        {
            _service = service;
        }
    }
}