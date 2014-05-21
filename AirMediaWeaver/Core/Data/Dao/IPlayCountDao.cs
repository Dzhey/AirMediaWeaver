

namespace AirMedia.Core.Data.Dao
{
    public interface IPlayCountDao
    {
        void UpdateTrackPlayCount(long trackId);
        void UpdateArtistPlayCount(string artistName);
        void UpdateAlbumPlayCount(string albumName);
        void UpdateGenrePlayCount(string genreName);
        int GetTrackPlayCount(long trackId);
        int GetArtistPlayCount(string artistName);
        int GetAlbumPlayCount(string albumName);
        int GetGenrePlayCount(string genreName);
    }
}