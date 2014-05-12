using AirMedia.Core.Data;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Log
{
    public class CountNewLogEntriesRequest : AbsRequest
    {
        public class RequestResult : BaseRequestResult
        {
            public int InfoEntriesCount { get; set; }
            public int WarnEntriesCount { get; set; }
            public int ErrorEntriesCount { get; set; }

            public RequestResult(int resultCode) : base(resultCode)
            {
            }
        }

        protected override BaseRequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var dao = (LogEntryDao) DatabaseHelper.Instance.GetDao<LogEntryRecord>();

            lock (InsertLogEntriesRequest.InsertMutex)
            {
                int lastReadEntryId = CoreUserPreferences.Instance.LastReadLogEntryId;
                int infoEntriesCount = dao.CountNewLogEntries(LogLevel.Info, lastReadEntryId);
                int warnEntriesCount = dao.CountNewLogEntries(LogLevel.Warning, lastReadEntryId);
                int errorEntriesCount = dao.CountNewLogEntries(LogLevel.Error, lastReadEntryId);

                return new RequestResult(BaseRequestResult.ResultCodeOk)
                {
                    ErrorEntriesCount = errorEntriesCount,
                    WarnEntriesCount = warnEntriesCount,
                    InfoEntriesCount = infoEntriesCount
                };
            }
        }
    }
}
