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
        ITrackMetadata[] QueryLocalForArtistNameLike(string artistName);

        /// <summary>
        /// </summary>
        /// <returns>local and remote tracks</returns>
        IRemoteTrackMetadata[] GetNotPlayedTracks();
    }
}