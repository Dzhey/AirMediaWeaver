using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Data;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class LocalLibraryPlaybackSource : IBasicPlaybackSource
    {
        public static readonly string LogTag = typeof (LocalLibraryPlaybackSource).Name;

        private int _currentPosition;
        private readonly LinkedList<long> _trackList;

        public static LocalLibraryPlaybackSource CreateFromParcel(LocalLibraryPlaybackSourceParcel parcel)
        {
            return new LocalLibraryPlaybackSource(parcel.TrackIds, parcel.CurrentPosition);
        }

        public LocalLibraryPlaybackSource()
        {
            _trackList = new LinkedList<long>();
        }

        public LocalLibraryPlaybackSource(params long[] trackIds)
            : this(trackIds, 0)
        {
        }

        public LocalLibraryPlaybackSource(int currentPosition, params long[] trackIds)
            : this(trackIds, currentPosition)
        {
            if (currentPosition < 0 || currentPosition > trackIds.Length)
            {
                throw new ArgumentException("inconsistent position and tracks passed");
            }
        }

        protected LocalLibraryPlaybackSource(long[] trackIds, int currentPosition)
        {
            _trackList = new LinkedList<long>(trackIds);
            _currentPosition = currentPosition;
        }

        public LocalLibraryPlaybackSourceParcel CreateParcelSource()
        {
            long[] trackIds = _trackList.ToArray();

            return new LocalLibraryPlaybackSourceParcel(_currentPosition, trackIds);
        }

        public bool HasCurrent()
        {
            return _trackList.Count > 0 && _currentPosition >= 0 && _currentPosition < _trackList.Count;
        }

        public bool HasNext()
        {
            return _currentPosition < _trackList.Count - 1;
        }

        public bool HasPrevious()
        {
            return _currentPosition > 0;
        }

        public bool MoveNext()
        {
            if (HasNext() == false) return false;

            _currentPosition++;

            AmwLog.Verbose(LogTag, string.Format("local source changed position to {0}", _currentPosition));

            return true;
        }

        public bool MovePrevious()
        {
            if (HasPrevious() == false) return false;

            _currentPosition--;

            AmwLog.Verbose(LogTag, string.Format("local source changed position to {0}", _currentPosition));

            return true;
        }

        public TrackMetadata? GetCurrentTrackMetadata()
        {
            CheckTrackPositionBounds();

            long trackId = _trackList.ElementAt(_currentPosition);

            return PlaylistDao.GetTrackMetadata(trackId);
        }

        public ResourceDescriptor? GetCurrentResource()
        {
            CheckTrackPositionBounds();

            return GetResourceImpl(_trackList.ElementAt(_currentPosition));
        }

        protected ResourceDescriptor? GetResourceImpl(long trackId)
        {
            var metadata = GetCurrentTrackMetadata();

            if (metadata == null)
            {
                AmwLog.Error(LogTag, "Can't retrieve track metadata for track id ({0})", trackId);
                return null;
            }

            var metadataVal = metadata.Value;

            return new ResourceDescriptor
                {
                    PublicGuid = null,
                    LocalId = metadataVal.TrackId,
                    Uri = new Uri(metadataVal.Data)
                };
        }

        private void CheckTrackPositionBounds()
        {
            if (_currentPosition < 0)
            {
                throw new ApplicationException(string.Format(
                    "unexpected current position value ({0})", _currentPosition));
            }

            if (_currentPosition >= _trackList.Count)
            {
                throw new ApplicationException("current track position exceeds track list");
            }
        }
    }
}