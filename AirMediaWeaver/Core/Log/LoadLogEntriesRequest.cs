using System.Collections.Generic;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using BaseRequestResult = AirMedia.Core.Requests.Model.RequestResult;

namespace AirMedia.Core.Log
{
    public class LoadLogEntriesRequest : BaseLoadRequest<List<LogEntryRecord>>
    {
        public class RequestResult : LoadRequestResult<List<LogEntryRecord>>
        {
            public RequestResult(int resultCode, List<LogEntryRecord> data) : 
                base(resultCode, data)
            {
            }

            public int LevelEntryCount { get; set; }
            public int TotalEntryCount { get; set; }
        }

        public LogLevel Level { get; private set; }

        public LoadLogEntriesRequest(LogLevel level)
        {
            Level = level;
        }

        protected override LoadRequestResult<List<LogEntryRecord>> DoLoad(out RequestStatus status)
        {
            lock (InsertLogEntriesRequest.InsertMutex)
            {
                status = RequestStatus.Ok;

                var dao = (LogEntryDao) DatabaseHelper.Instance.GetDao<LogEntryRecord>();

                var result = dao.QueryForLogLevel(Level);
                int levelEntryCount = dao.GetLogEntryCount(Level);
                int totalCount = dao.GetLogEntryCount();

                return new RequestResult(BaseRequestResult.ResultCodeOk, result)
                    {
                        LevelEntryCount = levelEntryCount,
                        TotalEntryCount = totalCount
                    };
            }
        }
    }
}
