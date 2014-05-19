using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Utils.StringUtils;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql;

namespace AirMedia.Core.Data
{
    public class TrackMetadataDao : ITrackMetadataDao
    {
        private readonly IAmwContentProvider _amwContentProvider;
        private readonly ITrackPublicationsDao _localPubDao;
        private readonly RemoteTrackPublicationsDao _pubDao;

        public TrackMetadataDao(ITrackPublicationsDao localPubDao, IAmwContentProvider amwContentProvider)
        {
            _localPubDao = localPubDao;
            _amwContentProvider = amwContentProvider;
            _pubDao = (RemoteTrackPublicationsDao) DatabaseHelper.Instance
                      .GetDao<RemoteTrackPublicationRecord>();
        }

        public static RemoteTrackMetadata[] CreateRemoteTracksMetadata<T>(IEnumerable<T> records) where T : IRemoteTrackMetadata
        {
            return records.Select(CreateRemoteTrackMetadata).ToArray();
        }

        public static RemoteTrackMetadata CreateRemoteTrackMetadata<T>(T record) where T : IRemoteTrackMetadata
        {
            return new RemoteTrackMetadata
            {
                TrackGuid = record.TrackGuid,
                PeerGuid = record.PeerGuid,
                Album = record.Album,
                Artist = record.Artist,
                TrackDurationMillis = record.TrackDurationMillis,
                TrackTitle = record.TrackTitle,
                ContentType = record.ContentType
            };
        }

        public static TrackMetadata[] CreateTracksMetadata<T>(IEnumerable<T> records) where T : ITrackMetadata
        {
            return records.Select(CreateTrackMetadata).ToArray();
        }

        public static TrackMetadata CreateTrackMetadata<T>(T record) where T : ITrackMetadata
        {
            return new TrackMetadata
            {
                TrackGuid = record.TrackGuid,
                PeerGuid = record.PeerGuid,
                Album = record.Album,
                Artist = record.Artist,
                TrackDurationMillis = record.TrackDurationMillis,
                TrackTitle = record.TrackTitle
            };
        }

        public static RemoteTrackPublicationRecord[] CreateRemotePublicationsRecord(
            IEnumerable<ITrackMetadata> records)
        {
            return records.Select(CreateRemotePublicationRecord).ToArray();
        }

        public static RemoteTrackPublicationRecord CreateRemotePublicationRecord(ITrackMetadata metadata)
        {
            return new RemoteTrackPublicationRecord
                {
                    TrackTitle = metadata.TrackGuid,
                    PeerGuid = metadata.PeerGuid,
                    Album = metadata.Album,
                    Artist = metadata.Artist,
                    TrackDurationMillis = metadata.TrackDurationMillis
                };
        }

        public void UpdateMetadata(IEnumerable<ITrackMetadata> metadata)
        {
            _pubDao.RedefineDatabaseRecords(CreateRemotePublicationsRecord(metadata));
        }

        public Uri GetRemoteTrackUri(string trackGuid)
        {
            const string template = "select {tPeers_cPeerAddress} " +
                                    "from {tPeers} " +
                                    "where {tPeers_cPeerGuid} in (" +
                                        "select {tTracks_cPeerGuid} " +
                                        "from {tTracks} " +
                                        "where {tTracks_cTrackGuid}='{trackGuid}' " +
                                        "limit 1)" +
                                    "limit 1";

            string query = template.HaackFormat(new
                {
                    tPeers = PeerRecord.TableName,
                    tPeers_cPeerAddress = PeerRecord.ColumnAddress,
                    tPeers_cPeerGuid = PeerRecord.ColumnPeerGuid,
                    tTracks = RemoteTrackPublicationRecord.TableName,
                    tTracks_cTrackGuid = RemoteTrackPublicationRecord.ColumnGuid, 
                    tTracks_cPeerGuid = RemoteTrackPublicationRecord.ColumnPeerGuid, 
                    trackGuid
                });

            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this))
            {
                var result = holder.Connection.Query<PeerRecord>(query).ToArray();

                if (result.Length == 0) return null;

                return _amwContentProvider.CreateRemoteTrackUri(
                    result[0].Address, 
                    Consts.DefaultHttpPort.ToString(CultureInfo.InvariantCulture), 
                    trackGuid);
            }
        }

        public ITrackMetadata GetTrackMetadata(string trackGuid)
        {
            var metadata = _localPubDao.QueryForGuid(trackGuid);

            if (metadata != null) return metadata;

            var pub = _pubDao.QueryForGuid(trackGuid);

            if (pub == null) return null;

            return CreateTrackMetadata(pub);
        }

        public IRemoteTrackMetadata[] GetRemoteTracksMetadata()
        {
            return _pubDao.GetAll().Select(CreateRemoteTrackMetadata).ToArray();
        }

        public IRemoteTrackMetadata GetRemoteTrackMetadata(string trackGuid)
        {
            var pub = _pubDao.QueryForGuid(trackGuid);

            if (pub == null) return null;

            return CreateRemoteTrackMetadata(pub);
        }
    }
}