using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using AirMedia.Core.Data.Dao;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Interfaces;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Data.Dao;
using SQLite;

namespace AirMedia.Core.Requests.Impl
{
    public class LoadRecommendationsRequest : BaseLoadCachedRequest<List<IRemoteTrackMetadata>>
    {
        public const string ActionTagDefault = "LoadRecommendationsRequest";
        public const string CacheEntryKey = "LoadRecommendationsRequest_cache_key";

        public LoadRecommendationsRequest(IRequestResultCache cache) : base(cache)
        {
        }

        protected override CachedLoadRequestResult<List<IRemoteTrackMetadata>> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var playCountDao = new AndroidPlayCountDao();

            var watch = new Stopwatch();
            watch.Start();
            var newTracks = RetrieveNewTracks()
                .OrderByDescending(metadata => GetRecommendationWeight(playCountDao, metadata))
                .ThenBy(metadata => metadata.Artist)
                .ThenBy(metadata => metadata.TrackTitle)
                .ThenBy(metadata => metadata.Album)
                .ToList();
            watch.Stop();
            AmwLog.Debug(LogTag, string.Format("recommendations sorted in {0} seconds", 
                watch.ElapsedMilliseconds / 1000));

            return new LoadRecommendationsRequestResult(RequestResult.ResultCodeOk, newTracks);
        }

        protected double GetRecommendationWeight(PlayCountDao dao, ITrackMetadata metadata)
        {
            double weight = 0;

            try
            {
                weight += dao.GetArtistPlayCount(metadata.Artist);
                weight += dao.GetAlbumPlayCount(metadata.Album) * 2;
                weight += dao.GetGenrePlayCount(metadata.Genre) * 0.01;
            }
            catch (SQLiteException e)
            {
                AmwLog.Warn(LogTag, "sql error computing recommendation weight", e.ToString());
            }

            return weight;
        }

        protected IRemoteTrackMetadata[] RetrieveNewTracks()
        {
            return DatabaseHelper.Instance.TrackMetadataDao.GetNotPlayedTracks();
        }

        protected override string GetCacheKey()
        {
            return CacheEntryKey;
        }
    }
}