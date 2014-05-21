using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Log;
using SQLite;

namespace AirMedia.Core.Data.Dao
{
    public abstract class PlayCountDao : IPlayCountDao
    {
        public static readonly string LogTag = typeof (PlayCountDao).Name;

        private readonly TrackPlayCountDao _trackPlayCountDao;
        private readonly ArtistPlayCountDao _artistPlayCountDao;
        private readonly AlbumPlayCountDao _albumPlayCountDao;
        private readonly GenrePlayCountDao _genrePlayCountDao;

        protected PlayCountDao()
        {
            _trackPlayCountDao = (TrackPlayCountDao) DatabaseHelper.Instance.GetDao<TrackPlayCountRecord>();
            _artistPlayCountDao = (ArtistPlayCountDao) DatabaseHelper.Instance.GetDao<ArtistPlayCountRecord>();
            _albumPlayCountDao = (AlbumPlayCountDao) DatabaseHelper.Instance.GetDao<AlbumPlayCountRecord>();
            _genrePlayCountDao = (GenrePlayCountDao) DatabaseHelper.Instance.GetDao<GenrePlayCountRecord>();
        }

        public void UpdatePlayCount(long trackId)
        {
            var metadata = GetTrackMetadata(trackId);

            if (metadata == null)
            {
                AmwLog.Error(LogTag, string.Format("can't update play count for track id: " +
                                     "track metadata not found; trackId: \"{0}\"", trackId));
                return;
            }

            UpdateTrackPlayCount(trackId);
            UpdatePlayCount(metadata);
        }

        protected abstract ITrackMetadata GetTrackMetadata(long trackId);

        public void UpdatePlayCount(string remoteTrackGuid)
        {
            var metadata = DatabaseHelper.Instance.TrackMetadataDao
                                         .GetRemoteTrackMetadata(remoteTrackGuid);
            if (metadata == null)
            {
                AmwLog.Error(LogTag, "can't update play count for remote track guid: " +
                                     "track metadata not found", remoteTrackGuid);
                return;
            }
            UpdateTrackPlayCount(remoteTrackGuid);
            UpdatePlayCount(metadata);
        }

        public void UpdateTrackPlayCount(string trackGuid)
        {
            if (_trackPlayCountDao.UpdateTrackPlayCount(trackGuid) < 1)
            {
                AmwLog.Error(LogTag, string.Format("can't update track play count for " +
                                                   "trackGuid: \"{0}\"", trackGuid));
            }
        }

        public void UpdateTrackPlayCount(long trackId)
        {
            if (_trackPlayCountDao.UpdateTrackPlayCount(trackId) < 1)
            {
                AmwLog.Error(LogTag, string.Format("can't update track play count for " +
                                                   "trackId: \"{0}\"", trackId));
            }
        }

        public void UpdateArtistPlayCount(string artistName)
        {
            AmwLog.Verbose(LogTag, string.Format("updating play count for artist \"{0}\"", artistName));
            try
            {
                if (_artistPlayCountDao.UpdateArtistPlayCount(artistName) < 1)
                {
                    AmwLog.Error(LogTag, string.Format("can't update artist play count for " +
                                                       "artistName: \"{0}\"", artistName));
                }
            }
            catch (SQLiteException e)
            {
                AmwLog.Warn(LogTag, string.Format("unable to update artist play " +
                                                  "count: \"{0}\"; SQL exception", artistName), e.ToString());
            }
        }

        public void UpdateAlbumPlayCount(string albumName)
        {
            AmwLog.Verbose(LogTag, string.Format("updating play count for album \"{0}\"", albumName));
            try
            {
                if (_albumPlayCountDao.UpdateAlbumPlayCount(albumName) < 1)
                {
                    AmwLog.Error(LogTag, string.Format("can't update album play count for " +
                                                       "albumName: \"{0}\"", albumName));
                }
            }
            catch (SQLiteException e)
            {
                AmwLog.Warn(LogTag, string.Format("unable to update album play " +
                                                  "count: \"{0}\"; SQL exception", albumName), e.ToString());
            }
        }

        public void UpdateGenrePlayCount(string genreName)
        {
            AmwLog.Verbose(LogTag, string.Format("updating play count for genre \"{0}\"", genreName));
            try 
            {
                if (_genrePlayCountDao.UpdateGenrePlayCount(genreName) < 1)
                {
                    AmwLog.Error(LogTag, string.Format("can't update genre play count for " +
                                                       "genreName: \"{0}\"", genreName));
                }
            }
            catch (SQLiteException e)
            {
                AmwLog.Warn(LogTag, string.Format("unable to update genre play " +
                                                  "count: \"{0}\"; SQL exception", genreName), e.ToString());
            }
        }

        public int GetTrackPlayCount(long trackId)
        {
            return _trackPlayCountDao.GetTrackPlayCount(trackId);
        }

        public int GetArtistPlayCount(string artistName)
        {
            return _artistPlayCountDao.GetArtistPlayCount(artistName);
        }

        public int GetAlbumPlayCount(string albumName)
        {
            return _albumPlayCountDao.GetAlbumPlayCount(albumName);
        }

        public int GetGenrePlayCount(string genreName)
        {
            return _genrePlayCountDao.GetGenrePlayCount(genreName);
        }

        protected void UpdatePlayCount(ITrackMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.Artist) == false)
            {
                UpdateArtistPlayCount(metadata.Artist);
            }
            else
            {
                AmwLog.Verbose(LogTag, "cant update artist play count: artist " +
                                       "name is not defined", metadata.ToString());
            }

            if (string.IsNullOrWhiteSpace(metadata.Album) == false)
            {
                UpdateAlbumPlayCount(metadata.Album);
            }
            else
            {
                AmwLog.Verbose(LogTag, "cant update album play count: album " +
                                       "name is not defined", metadata.ToString());
            }

            if (string.IsNullOrWhiteSpace(metadata.Genre) == false)
            {
                UpdateGenrePlayCount(metadata.Genre);
            }
            else
            {
                AmwLog.Verbose(LogTag, "cant update genre play count: genre " +
                                       "name is not defined", metadata.ToString());
            }
        }
    }
}