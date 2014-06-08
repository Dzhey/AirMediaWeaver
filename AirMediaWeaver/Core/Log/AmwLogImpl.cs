using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Log
{
    public class AmwLogImpl : AmwLog, IRequestResultListener
    {
        private const string ActionTagInsertLogEntries = "TmLogImpl_insert_log_entries";

        private static readonly string LogTag = typeof (AmwLogImpl).Name;
        private static readonly object Mutex = new object();

        private volatile bool _isRequestHandlerRegistered;
        private bool _isDisposed;

        protected override void DispatchLogRequest(LogLevel level, string tag, 
            string displayMessage, string details, params object[] args)
        {
            if (Consts.IsInAppLoggingEnabled && _isRequestHandlerRegistered == false)
            {
                lock (Mutex)
                {
                    if (_isRequestHandlerRegistered == false)
                    {
                        RequestManager.Instance.RegisterEventHandler(this);
                        _isRequestHandlerRegistered = true;
                    }
                }
            }

            base.DispatchLogRequest(level, tag, displayMessage, details, args);
        }

        protected override void PerformEntryLog(LogEntry entry)
        {
            System.Diagnostics.Debug.WriteLine(entry);
        }

        protected override void SaveEntries(IEnumerable<LogEntry> entries)
        {
            if (DatabaseHelper.Instance.IsInitialized == false)
            {
                Android.Util.Log.Warn(LogTag, "skipped log entries: database is not ready yet");

                return;
            }

            var records = entries.Select(entry => new LogEntryRecord
                {
                    Date = entry.LogDate,
                    Details = entry.Details,
                    Level = entry.Level,
                    Message = entry.DisplayMessage,
                    Tag = entry.Tag
                });

            var rq = new InsertLogEntriesRequest(records) {ActionTag = ActionTagInsertLogEntries};
            RequestManager.Instance.SubmitRequest(rq, true);
        }

        public void HandleRequestResult(object sender, ResultEventArgs args)
        {
            if (args.Request is InsertLogEntriesRequest && args.Request.Status != RequestStatus.Ok)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Failed to save log entries (message:\"{0}\")", args.Result.ErrorMessage);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_isDisposed) return;

            if (disposing)
            {
                RequestManager.Instance.RemoveEventHandler(this);
            }

            _isDisposed = true;
        }
    }
}
