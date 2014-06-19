

namespace AirMedia.Core.Data.Model
{
    public struct ArtistAlbums
    {
        public ArtistBaseModel Artist { get; set; }
        public AlbumBaseModel[] Albums { get; set; }
    }
}