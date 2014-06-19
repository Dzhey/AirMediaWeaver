using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Data;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class MixedPlaybackSource : IBasicPlaybackSource
    {
        public static readonly string LogTag = typeof(RemotePlaybackSource).Name;

        private int _encodedPosition;
        private readonly LinkedList<int> _playbackPositions;
        private readonly LinkedList<string> _remoteTracksList;
        private readonly LinkedList<long> _localTracksList;
        private readonly ITrackMetadataDao _trackMetadataDao;

        public static MixedPlaybackSource CreateFromParcel(ITrackMetadataDao trackMetadataDao,
            MixedPlaybackSourceParcel parcel)
        {
            return new MixedPlaybackSource(trackMetadataDao, parcel);
        }

        public MixedPlaybackSource(ITrackMetadataDao trackMetadataDao)
        {
            _trackMetadataDao = trackMetadataDao;

            _remoteTracksList = new LinkedList<string>();
            _localTracksList = new LinkedList<long>();
            _playbackPositions = new LinkedList<int>();
        }

        protected MixedPlaybackSource(ITrackMetadataDao trackMetadataDao,
            MixedPlaybackSourceParcel parcel)
        {
            _trackMetadataDao = trackMetadataDao;

            _localTracksList = new LinkedList<long>(parcel.TrackIds);
            _remoteTracksList = new LinkedList<string>(parcel.TrackGuids);
            _playbackPositions = new LinkedList<int>(parcel.Positions);
            _encodedPosition = parcel.EncodedPosition;
        }

        public bool HasCurrent()
        {
            return _playbackPositions.Count > 0 
                && _encodedPosition >= 0
                && _encodedPosition < _playbackPositions.Count;
        }

        public bool HasNext()
        {
            return _encodedPosition < _playbackPositions.Count - 1;
        }

        public bool HasPrevious()
        {
            return _encodedPosition > 0;
        }

        public bool MoveNext()
        {
            if (HasNext() == false) return false;

            _encodedPosition++;

            AmwLog.Verbose(LogTag, string.Format("mixed source changed position to {0}", _encodedPosition));

            return true;
        }

        public bool MovePrevious()
        {
            if (HasPrevious() == false) return false;

            _encodedPosition--;

            AmwLog.Verbose(LogTag, string.Format("mixed source changed position to {0}", _encodedPosition));

            return true;
        }

        public ResourceDescriptor? GetCurrentResource()
        {
            CheckTrackPositionBounds();

            return GetResourceImpl(_playbackPositions.ElementAt(_encodedPosition));
        }

        protected ResourceDescriptor? GetResourceImpl(int encodedPosition)
        {
            int? pos = DecodeLocalResourcePosition(encodedPosition);

            if (pos != null)
            {
                var localTrackMetadata = GetLocalTrackMetadata((int) pos);

                if (localTrackMetadata == null)
                {
                    AmwLog.Error(LogTag, "can't retrieve local track metadata; position: \"{0}\"", encodedPosition);

                    return null;
                }

                return new ResourceDescriptor
                    {
                        LocalId = localTrackMetadata.Value.TrackId,
                        Uri = new Uri(localTrackMetadata.Value.Data)
                    };
            }

            pos = DecodeRemoteResourcePosition(encodedPosition);
            if (pos == null)
            {
                AmwLog.Error(LogTag, "can't decode playback resource position: \"{0}\"", encodedPosition);

                return null;
            }

            var metadata = GetRemoteTrackMetadata((int) pos);

            if (metadata == null)
            {
                AmwLog.Error(LogTag, "can't retrieve remote track metadata; position: \"{0}\"", encodedPosition);

                return null;
            }

            var uri = _trackMetadataDao.GetRemoteTrackUri(metadata.TrackGuid);

            if (uri == null)
            {
                AmwLog.Error(LogTag, (object)null, "Can't retrieve track uri for " +
                                                   "track guid ({0}); position: \"{1}\"", 
                                                   metadata.TrackGuid, _encodedPosition);
                return null;
            }

            return new ResourceDescriptor
                {
                    PublicGuid = metadata.TrackGuid,
                    Uri = uri
                };
        }

        private int? DecodeLocalResourcePosition(int position)
        {
            if (_localTracksList.Count < 1) return null;

            if (position < 0 || position >= _localTracksList.Count)
            {
                return null;
            }

            return position;
        }

        private int? DecodeRemoteResourcePosition(int position)
        {
            if (_remoteTracksList.Count < 1) return null;

            int pos =  position - _localTracksList.Count;

            if (pos < -1 || pos >= (_localTracksList.Count + _remoteTracksList.Count))
            {
                return null;
            }

            return pos;
        }

        public TrackMetadata? GetLocalTrackMetadata(int decodedPosition)
        {
            long trackId = _localTracksList.ElementAt(decodedPosition);

            return PlaylistDao.GetTrackMetadata(trackId);
        }

        public ITrackMetadata GetRemoteTrackMetadata(int decodedPosition)
        {
            string trackGuid = _remoteTracksList.ElementAt(decodedPosition);

            return _trackMetadataDao.GetRemoteTrackMetadata(trackGuid);
        }

        private void CheckTrackPositionBounds()
        {
            if (_encodedPosition < 0)
            {
                throw new ApplicationException(string.Format(
                    "unexpected current position value ({0})", _encodedPosition));
            }

            if (_encodedPosition >= (_localTracksList.Count + _remoteTracksList.Count))
            {
                throw new ApplicationException("current track position exceeds track list");
            }
        }
    }
}