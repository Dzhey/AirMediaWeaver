using System;
using AirMedia.Core.Data.Model;

namespace AirMedia.Core.Data
{
    public interface ITrackMetadataDao
    {
        Uri GetTrackUri(string trackGuid);
        ITrackMetadata GetTrackMetadata(string trackGuid);
        ITrackMetadata[] GetRemoteTracksMetadata();
    }
}