using System;
using AirMedia.Core.Data.Model;

namespace AirMedia.Core.Data
{
    public interface ITrackMetadataDao
    {
        Uri GetRemoteTrackUri(string trackGuid);
        ITrackMetadata GetTrackMetadata(string trackGuid);
        IRemoteTrackMetadata[] GetRemoteTracksMetadata();
        IRemoteTrackMetadata GetRemoteTrackMetadata(string trackGuid);
    }
}