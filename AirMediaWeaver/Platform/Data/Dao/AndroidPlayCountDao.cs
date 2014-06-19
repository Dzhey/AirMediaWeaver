using AirMedia.Core.Data.Dao;
using AirMedia.Core.Data.Model;
using AirMedia.Platform.Controller.Dao;

namespace AirMedia.Platform.Data.Dao
{
    public class AndroidPlayCountDao : PlayCountDao
    {
        protected override ITrackMetadata GetTrackMetadata(long trackId)
        {
            return PlaylistDao.GetTrackMetadata(trackId);
        }
    }
}