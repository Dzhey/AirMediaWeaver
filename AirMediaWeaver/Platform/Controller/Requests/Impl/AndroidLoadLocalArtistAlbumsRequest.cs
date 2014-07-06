using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.Encodings;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.UI.Library.AlbumList;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class AndroidLoadLocalArtistAlbumsRequest : AbsLoadLocalArtistAlbumsRequest<AlbumListEntry>
    {
        public const int AlbumArtsCount = 8;
        public const string ActionTagDefault = "AndroidLoadLocalArtistAlbumsRequest_tag";

        public AndroidLoadLocalArtistAlbumsRequest() : 
            base(App.MemoryRequestResultCache, App.DatabaseHelper.TrackMetadataDao)
        {
        }

        protected override CachedLoadRequestResult<List<AlbumListEntry>> DoLoad(out RequestStatus status)
        {
            var baseResult = base.DoLoad(out status);

            // Preload album thumbnails
            var albumArts = new KeyValuePair<long, Bitmap>[AlbumArtsCount];

            var data = baseResult.Data;
            for (int i = 0, n = 0; n < albumArts.Length && i < data.Count; i++)
            {
                foreach (var album in data[i].Albums)
                {
                    var rq = new LoadAlbumArtRequest(album.AlbumId);
                    var result = (LoadRequestResult<Bitmap>) rq.Execute();

                    if (result.Data != null)
                    {
                        albumArts[n] = new KeyValuePair<long, Bitmap>(album.AlbumId, result.Data);
                        result.Data = null;
                        n++;

                        if (n >= albumArts.Length)
                            break;
                    }
                }
            }

            return new LoadArtistAlbumsRequestResult(RequestResult.ResultCodeOk)
                {
                    Data = baseResult.Data,
                    AlbumArts = albumArts
                };
        }

        protected override AlbumListEntry CreateItem(ArtistBaseModel artist, AlbumBaseModel[] albums)
        {
            var entry = new AlbumListEntry
                {
                    ArtistName = artist.ArtistName,
                    Albums = albums.Select(item => new AlbumGridItem
                        {
                            AlbumName = item.AlbumName,
                            AlbumId = item.AlbumId
                        }).ToArray()
                };

            return entry;
        }
    }
}