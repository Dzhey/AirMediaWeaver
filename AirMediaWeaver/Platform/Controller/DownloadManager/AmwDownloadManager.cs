using System;
using AirMedia.Core.Controller.DownloadManager;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.WebService.Http;
using AirMedia.Platform.Data.Sql.Dao;
using AirMedia.Platform.Data.Sql.Model;
using Android.App;
using Android.Content;
using AndroidDownloadManager = Android.App.DownloadManager;
using AndroidDownloadStatus = Android.App.DownloadStatus;
using DownloadStatus = AirMedia.Core.Controller.DownloadManager.DownloadStatus;
using Uri = Android.Net.Uri;

namespace AirMedia.Platform.Controller.DownloadManager
{
    public class AmwDownloadManager : ITrackDownloadsManager, IDisposable
    {
        public static readonly string LogTag = typeof (AmwDownloadManager).Name;

        private AndroidDownloadManager _downloadManager;
        private readonly TrackDownloadsDao _trackDownloadsDao;
        private bool _isDisposed;

        public static AmwDownloadManager NewInstance()
        {
            var trackDownloadsDao = (TrackDownloadsDao)DatabaseHelper.Instance.GetDao<TrackDownloadRecord>();

            return new AmwDownloadManager(App.Instance, trackDownloadsDao);
        }

        public AmwDownloadManager(Context context, TrackDownloadsDao downloadsDao)
        {
            _downloadManager = (AndroidDownloadManager) context.GetSystemService(Context.DownloadService);
            _trackDownloadsDao = downloadsDao;
        }

        public bool IsTrackDownloadPresented(string trackGuid)
        {
            var presentDownload = _trackDownloadsDao.QueryForGuid(trackGuid);
            if (presentDownload != null)
            {
                int downloadStatus = GetDownloadStatus(presentDownload.DownloadId);
                if (downloadStatus == DownloadStatus.Unknown
                    || downloadStatus == DownloadStatus.Failed)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public bool IsTrackDownloaded(string trackGuid)
        {
            return GetDownloadStatus(trackGuid, AndroidDownloadStatus.Successful) == DownloadStatus.Successful;
        }

        public int GetDownloadStatus(long downloadId, AndroidDownloadStatus? filterByStatus = null)
        {
            var query = new AndroidDownloadManager.Query();
            query.SetFilterById(downloadId);

            if (filterByStatus != null)
            {
                query.SetFilterByStatus(filterByStatus.Value);
            }

            using (var cursor = _downloadManager.InvokeQuery(query))
            {
                try
                {
                    if (cursor.MoveToFirst() == false)
                    {
                        AmwLog.Error(LogTag, "can't retrieve download status: download not " +
                                             "found; download id: \"{0}\"", downloadId);

                        return DownloadStatus.Unknown;
                    }

                    int idx = cursor.GetColumnIndex(AndroidDownloadManager.ColumnStatus);
                    var status = (AndroidDownloadStatus) cursor.GetInt(idx);

                    return TranslateDownloadStatus(status);
                }
                finally
                {
                    cursor.Close();
                }
            }
        }

        public int GetDownloadStatus(string trackGuid)
        {
            return GetDownloadStatus(trackGuid, null);
        }

        public int GetDownloadStatus(string trackGuid, AndroidDownloadStatus? filterByStatus)
        {
            var record = _trackDownloadsDao.QueryForGuid(trackGuid);
            if (record == null)
            {
                AmwLog.Warn(LogTag, string.Format("can't find download record for track " +
                                                  "guid \"{0}\"", trackGuid));
                return DownloadStatus.Unknown;
            }

            return GetDownloadStatus(record.DownloadId, filterByStatus);
        }

        /// <summary>
        /// </summary>
        /// <param name="trackGuid"></param>
        public void EnqueueDownload(string trackGuid)
        {
            var metadataDao = DatabaseHelper.Instance.TrackMetadataDao;
            var metadata = metadataDao.GetRemoteTrackMetadata(trackGuid);
            var uri = metadataDao.GetRemoteTrackUri(trackGuid);

            if (uri == null || metadata == null)
            {
                AmwLog.Error(LogTag, (object) trackGuid, "can't begin track publication " +
                                                         "download: publication uri not found");

                throw new ArgumentException("requested track publication not found");
            }

            if (IsTrackDownloadPresented(trackGuid))
            {
                AmwLog.Warn(LogTag, "track is already enqueued to download", trackGuid);
                return;
            }

            var presentDownload = _trackDownloadsDao.QueryForGuid(trackGuid);

            var provider = new HttpContentProvider();
            var dstUri = Uri.Parse(provider.CreateTrackDownloadDestinationUri(metadata).ToString());
            var request = new AndroidDownloadManager.Request(Uri.Parse(uri.ToString()))
                                                    .SetAllowedNetworkTypes(DownloadNetwork.Wifi)
                                                    .SetAllowedOverRoaming(false)
                                                    .SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted)
                                                    .SetVisibleInDownloadsUi(true)
                                                    .SetDestinationUri(dstUri);
            request.AllowScanningByMediaScanner();
            long downloadId = _downloadManager.Enqueue(request);

            if (presentDownload == null)
            {
                presentDownload = new TrackDownloadRecord();
            }

            presentDownload.TrackGuid = trackGuid;
            presentDownload.DownloadId = downloadId;
            int ret = _trackDownloadsDao.Insert(presentDownload);

            if (ret < 1)
            {
                AmwLog.Error(LogTag, (object)trackGuid, "failed to register enqueued track download");
            }
            else
            {
                AmwLog.Info(LogTag, "track download sucessfully enqueued", trackGuid);
            }
        }

        private int TranslateDownloadStatus(AndroidDownloadStatus downloadStatus)
        {
            switch (downloadStatus)
            {
                case AndroidDownloadStatus.Pending:
                    return DownloadStatus.Pending;

                case AndroidDownloadStatus.Running:
                    return DownloadStatus.Running;

                case AndroidDownloadStatus.Paused:
                    return DownloadStatus.Paused;

                case AndroidDownloadStatus.Successful:
                    return DownloadStatus.Successful;

                case AndroidDownloadStatus.Failed:
                    return DownloadStatus.Failed;

                default:
                    AmwLog.Error(LogTag, "can't translate download status: \"{0}\"", downloadStatus);
                    return DownloadStatus.Unknown;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _downloadManager = null;
            }

            _isDisposed = true;
        }
    }
}