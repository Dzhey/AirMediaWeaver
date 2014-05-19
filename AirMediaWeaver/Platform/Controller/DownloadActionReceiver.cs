using AirMedia.Core.Log;
using AirMedia.Platform.Controller.DownloadManager;
using AirMedia.Platform.UI.MainView;
using AirMedia.Platform.UI.Publications;
using Android.App;
using Android.Content;
using AndroidDownloadManager = Android.App.DownloadManager;
using DownloadStatus = AirMedia.Core.Controller.DownloadManager.DownloadStatus;

namespace AirMedia.Platform.Controller
{
    [BroadcastReceiver(Exported = true, Enabled = true)]
    [IntentFilter(new[]
        {
            AndroidDownloadManager.ActionDownloadComplete,
            AndroidDownloadManager.ActionNotificationClicked
        })]
    public class DownloadActionReceiver : BroadcastReceiver
    {
        public static readonly string LogTag = typeof (DownloadActionReceiver).Name;

        public override void OnReceive(Context context, Intent intent)
        {
            AmwLog.Info(LogTag, string.Format("received download intent: \"{0}\"", intent.Action));

            switch (intent.Action)
            {
                case AndroidDownloadManager.ActionDownloadComplete:
                    HandleDownloadCompletion(intent.GetLongExtra(AndroidDownloadManager.ExtraDownloadId, 0));
                    break;

                case AndroidDownloadManager.ActionNotificationClicked:
                    var startIntent = new Intent(context, typeof(MainViewActivity));
                    startIntent.AddFlags(ActivityFlags.BroughtToFront 
                        | ActivityFlags.ClearTask 
                        | ActivityFlags.NewTask);
                    startIntent.PutExtra(MainViewActivity.ExtraDisplayFragment, 
                        typeof (PublicationsTabFragment).FullName);
                    context.StartActivity(startIntent);
                    break;

                default:
                    AmwLog.Error(LogTag, "unexpected download intent not handled ");
                    break;
            }
        }

        private void HandleDownloadCompletion(long downloadId)
        {
            var downloadManager = AmwDownloadManager.NewInstance();
            var downloadStatus = downloadManager.GetDownloadStatus(downloadId);

            if (downloadStatus == DownloadStatus.Unknown)
            {
                AmwLog.Error(LogTag, string.Format("can't retrieve download status for " +
                                                  "download id: \"{0}\"", downloadId));
                return;
            }

            AmwLog.Info(LogTag, string.Format("download status: {0}", 
                DownloadStatus.GetString(downloadStatus)));
        }
    }
}