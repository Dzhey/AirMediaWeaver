using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Controller;
using Android.Content;
using Android.OS;

namespace AirMedia.Platform.Controller
{
    public class WorkerRequestManager : RequestManager
    {
        public Context Context { get; private set; }
        public Handler Handler { get; private set; }

        public WorkerRequestManager(Context context)
        {
            Context = context;
            Handler = new Handler();
        }
            
        /// <returns>generated request id</returns>
        protected override void SubmitRequestImpl(AbsRequest request, int requestId, bool isParallel)
        {
            App.Instance.StartService(CreateIntent(Handler, requestId, isParallel, request));
        }

        protected Intent CreateIntent(Handler handler, int requestId, bool isParallel, AbsRequest request)
        {
            var intent = new Intent(App.Instance, typeof(WorkerService));
            intent.SetAction(WorkerService.ActionProcessRequest);
            intent.SetPackage(App.Instance.PackageName);
            intent.PutExtra(WorkerService.ExtraRequestId, requestId);
            intent.PutExtra(WorkerService.ExtraIsParallelRequest, isParallel);

            return intent;
        }
    }
}