using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Dao;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class LoadAlbumArtRequest : BaseLoadRequest<Bitmap>
    {
        public const string ActionTagDefault = "LoadAlbumArtRequest_tag";

        public long AlbumId { get; private set; }

        public LoadAlbumArtRequest(long albumId)
        {
            AlbumId = albumId;
        }

        protected override LoadRequestResult<Bitmap> DoLoad(out RequestStatus status)
        {
            var bitmap = AlbumsDao.GetAlbumArt(AlbumId);

            if (bitmap != null)
            {
                int delta = bitmap.Width - bitmap.Height;
                if (delta > 0)
                {
                    int x = delta/2;
                    int dstWidth = bitmap.Width - delta;
                    int dstHeight = bitmap.Height;
                    var tmp = bitmap;
                    bitmap = Bitmap.CreateBitmap(bitmap, x, 0, dstWidth, dstHeight);
                    tmp.Recycle();
                }
            }

            status = RequestStatus.Ok;

            return new LoadRequestResult<Bitmap>(RequestResult.ResultCodeOk, bitmap);
        }
    }
}