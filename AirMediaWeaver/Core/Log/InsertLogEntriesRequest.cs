using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Core.Utils;

namespace AirMedia.Core.Log
{
    public class InsertLogEntriesRequest : AbsRequest
    {
        public class RequestResult : Requests.Model.RequestResult
        {
            public LogEntryRecord[] Entries { get; set; }

            public RequestResult(int resultCode) : base(resultCode)
            {
            }
        }

        private readonly IEnumerable<LogEntryRecord> _entries;

        public static readonly object InsertMutex = new object();

        public InsertLogEntriesRequest(IEnumerable<LogEntryRecord> entries)
        {
            _entries = entries;
        }

        protected override Requests.Model.RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            Debug.WriteLine("Inserting log entries");

            var dao = (LogEntryDao) DatabaseHelper.Instance.GetDao<LogEntryRecord>();

            var entries = _entries.ToArray();

            if (entries.Length < 1)
            {
                return new RequestResult(Requests.Model.RequestResult.ResultCodeOk)
                {
                    Entries = new LogEntryRecord[0]
                };
            }

            lock (InsertMutex)
            {
                using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this, 
                    DatabaseHelper.Instance.LogDatabasePath))
                {
                    var proc = holder.Connection.CreateTransactionProcedure(
                        conn => dao.InsertAll(entries, conn) != 0);

                    if (proc() == false)
                    {
                        AmwLog.Error(LogTag, "Can't insert log entries");
                    }
                }
            }

            int count = dao.GetEntryCount();

            if (count >= Consts.MaxInAppLoggingCacheEntries)
            {
                int entriesToRemove = count - Consts.MaxInAppLoggingCacheEntries;

                if (entriesToRemove > Consts.InApplogCleanThreshold)
                {
                    lock (InsertMutex)
                    {
                        using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this,
                                DatabaseHelper.Instance.LogDatabasePath))
                        {
                            var proc = holder.Connection.CreateTransactionProcedure(conn =>
                                {
                                    int countDeleted = dao.DeleteTopEntries(entriesToRemove, conn);

                                    AmwLog.Verbose(LogTag, string.Format("{0} log entries deleted from {1} entries; (limit is {2})",
                                                    countDeleted, count, Consts.MaxInAppLoggingCacheEntries));

                                    return countDeleted > 0;
                                });
                            proc();
                        }
                    }
                }
            }

            Debug.WriteLine("Log entries insert finished");

            return new RequestResult(Requests.Model.RequestResult.ResultCodeOk)
                {
                    Entries = entries
                };
        }
    }
}
