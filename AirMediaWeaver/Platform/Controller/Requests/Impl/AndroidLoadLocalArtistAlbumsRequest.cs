using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Requests.Impl;
using AirMedia.Platform.UI.Library.AlbumList;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class AndroidLoadLocalArtistAlbumsRequest : AbsLoadLocalArtistAlbumsRequest<AlbumListEntry>
    {
        public const string ActionTagDefault = "AndroidLoadLocalArtistAlbumsRequest_tag";

        public AndroidLoadLocalArtistAlbumsRequest() : 
            base(App.MemoryRequestResultCache, App.DatabaseHelper.TrackMetadataDao)
        {
        }

        protected override AlbumListEntry CreateItem(ArtistBaseModel artist, AlbumBaseModel[] albums)
        {
            return new AlbumListEntry
                {
                    ArtistName = artist.ArtistName,
                    Albums = albums.Select(item => new AlbumGridItem
                        {
                            AlbumName = item.AlbumName,
                            AlbumId = item.AlbumId
                        }).ToArray()
                };
        }
    }
}