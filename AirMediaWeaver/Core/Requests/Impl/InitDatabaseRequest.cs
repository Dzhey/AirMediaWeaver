using AirMedia.Core.Data;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Impl
{
    public class InitDatabaseRequest : AbsRequest 
    {
        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            lock (DatabaseHelper.Instance)
            {
                if (CoreUserPreferences.Instance.DatabaseCreated)
                {
                    return new RequestResult(RequestResult.ResultCodeOk);
                }

                DatabaseHelper.Instance.CreateDatabase();
                CoreUserPreferences.Instance.DatabaseCreated = true;
            }

            return new RequestResult(RequestResult.ResultCodeOk);
        }
    }
}
