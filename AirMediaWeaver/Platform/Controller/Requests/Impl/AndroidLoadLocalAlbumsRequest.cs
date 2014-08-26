using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data.Model;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Requests.Model;
using AirMedia.Platform.UI.Library.AlbumList.Model;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class AndroidLoadLocalAlbumsRequest : AbsLoadLocalAlbumsRequest<AlbumGridItem>
    {
        public const int AlbumArtsCount = 8;
        public const string ActionTagDefault = "AndroidLoadLocalAlbumsRequest_tag";

        public AndroidLoadLocalAlbumsRequest() : 
            base(App.MemoryRequestResultCache, App.DatabaseHelper.TrackMetadataDao)
        {
        }

        protected override CachedLoadRequestResult<List<AlbumGridItem>> DoLoad(out RequestStatus status)
        {
            var baseResult = base.DoLoad(out status);

            // Preload album thumbnails
            var albumArts = new KeyValuePair<long, Bitmap>[AlbumArtsCount];

            var data = baseResult.Data;
            for (int i = 0, n = 0; n < albumArts.Length && i < data.Count; i++)
            {
                var rq = new LoadAlbumArtRequest(data[i].AlbumId);
                var result = (LoadRequestResult<Bitmap>) rq.Execute();

                if (result.Data != null)
                {
                    albumArts[n] = new KeyValuePair<long, Bitmap>(data[i].AlbumId, result.Data);
                    result.Data = null;
                    n++;

                    if (n >= albumArts.Length)
                        break;
                }
            }

            return new LoadLocalAlbumsRequestResult(RequestResult.ResultCodeOk)
                {
                    Data = baseResult.Data,
                    AlbumArts = albumArts
                };
        }

        protected override List<AlbumGridItem> CreateItems(AlbumBaseModel[] albums)
        {
            return albums.Select(item => new AlbumGridItem
                            {
                                AlbumId = item.AlbumId,
                                AlbumName = item.AlbumName
                            }).ToList();
        }
    }
}