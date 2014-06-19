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
        ITrackMetadata[] QueryLocalTracksForTitleLike(string trackTitle);
        ITrackMetadata[] QueryLocalTracksForArtistNameLike(string artistName);
        ITrackMetadata[] QueryLocalTracksForAlbumNameLike(string artistName);
        ITrackMetadata[] QueryLocalTracksForGenreNameLike(string genreName);
        IRemoteTrackMetadata[] QueryRemoteTracksForTitleLike(string trackTitle);
        IRemoteTrackMetadata[] QueryRemoteTracksForArtistNameLike(string artistName);
        IRemoteTrackMetadata[] QueryRemoteTracksForAlbumNameLike(string artistName);
        IRemoteTrackMetadata[] QueryRemoteTracksForGenreNameLike(string genreName);
        ArtistBaseModel[] QueryForLocalArtists();
        AlbumBaseModel[] QueryForArtistAlbums(long artistId);

        /// <summary>
        /// </summary>
        /// <returns>local and remote tracks</returns>
        IRemoteTrackMetadata[] GetNotPlayedTracks();
    }
}