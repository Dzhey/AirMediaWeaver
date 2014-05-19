using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data;
using AirMedia.Core.Log;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class RemotePlaybackSource : IBasicPlaybackSource
    {
        public static readonly string LogTag = typeof(RemotePlaybackSource).Name;

        private int _currentPosition;
        private readonly LinkedList<string> _trackList;
        private readonly ITrackMetadataDao _trackMetadataDao;

        public static RemotePlaybackSource CreateFromParcel(ITrackMetadataDao trackMetadataDao, 
            RemotePlaybackSourceParcel parcel)
        {
            return new RemotePlaybackSource(trackMetadataDao, 
                parcel.TrackGuids, parcel.CurrentPosition);
        }

        public RemotePlaybackSource(ITrackMetadataDao trackMetadataDao)
        {
            _trackList = new LinkedList<string>();
            _trackMetadataDao = trackMetadataDao;
        }

        public RemotePlaybackSource(ITrackMetadataDao trackMetadataDao, params string[] trackGuids)
            : this(trackMetadataDao, trackGuids, 0)
        {
        }

        public RemotePlaybackSource(ITrackMetadataDao trackMetadataDao, 
            int currentPosition, params string[] trackGuids)
            : this(trackMetadataDao, trackGuids, currentPosition)
        {
            if (currentPosition < 0 || currentPosition > trackGuids.Length)
            {
                throw new ArgumentException("inconsistent position and tracks passed");
            }
        }

        protected RemotePlaybackSource(ITrackMetadataDao trackMetadataDao, 
            string[] trackGuids, int currentPosition)
        {
            _trackList = new LinkedList<string>(trackGuids);
            _currentPosition = currentPosition;
            _trackMetadataDao = trackMetadataDao;
        }

        public RemotePlaybackSourceParcel CreateParcelSource()
        {
            string[] trackGuids = _trackList.ToArray();

            return new RemotePlaybackSourceParcel(_currentPosition, trackGuids);
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

        public ResourceDescriptor? GetCurrentResource()
        {
            CheckTrackPositionBounds();

            return GetResourceImpl(_trackList.ElementAt(_currentPosition));
        }

        protected ResourceDescriptor? GetResourceImpl(string trackGuid)
        {
            var uri = _trackMetadataDao.GetRemoteTrackUri(trackGuid);

            if (uri == null)
            {
                AmwLog.Error(LogTag, string.Format("Can't retrieve track uri for " +
                                                   "track guid ({0})", trackGuid));
                return null;
            }

            return new ResourceDescriptor
                {
                    PublicGuid = trackGuid,
                    Uri = uri
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