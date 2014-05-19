using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Requests.Model;

namespace AirMedia.Core.Requests.Abs
{
    public abstract class UpdateRemoteTrackPublicationsRequest : AbsRequest
    {
        protected override RequestResult ExecuteImpl(out RequestStatus status)
        {
            var metadata = DownloadRemoteTrackPublications();
            var pubDao = (RemoteTrackPublicationsDao)DatabaseHelper.Instance
                                .GetDao<RemoteTrackPublicationRecord>();
            pubDao.UpdateRemoteTrackPublications(metadata);

            status = RequestStatus.Ok;

            return RequestResult.ResultOk;
        }

        protected abstract ITrackMetadata[] DownloadRemoteTrackPublications();
    }
}