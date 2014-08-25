using AirMedia.Core.Log;
using AirMedia.Core.Requests.Abs;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller.Dao;
using AirMedia.Platform.Util;
using Android.Graphics;

namespace AirMedia.Platform.Controller.Requests.Impl
{
    public class LoadAlbumArtRequest : BaseLoadRequest<Bitmap>
    {
        public const string ActionTagDefault = "LoadAlbumArtRequest_tag";

        private readonly IReuseBitmapCacheAccessor _reuseBitmapCacheAccessor;

        public long AlbumId { get; private set; }

        public LoadAlbumArtRequest(long albumId, IReuseBitmapCacheAccessor reuseBitmapCacheAccessor = null)
        {
            AlbumId = albumId;
            _reuseBitmapCacheAccessor = reuseBitmapCacheAccessor;
        }

        protected override LoadRequestResult<Bitmap> DoLoad(out RequestStatus status)
        {
            status = RequestStatus.Ok;

            var options = new BitmapFactory.Options {InJustDecodeBounds = true, InSampleSize = 1};
            AlbumsDao.GetAlbumArtBitmap(AlbumId, options);

            if (options.OutWidth < 1)
            {
                return new LoadRequestResult<Bitmap>(RequestResult.ResultCodeOk, null);
            }

            if (_reuseBitmapCacheAccessor != null)
            {
                _reuseBitmapCacheAccessor.AddInBitmapOptions(options);
                if (options.InBitmap != null)
                {
                    AmwLog.Info(LogTag, "bitmap reused for album id: " + AlbumId);
                }
            }
            Bitmap bitmap = AlbumsDao.GetAlbumArtBitmap(AlbumId);

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

                    if (_reuseBitmapCacheAccessor == null)
                    {
                        tmp.Recycle();
                        tmp.Dispose();
                    }
                    else
                    {
                        _reuseBitmapCacheAccessor.AddReusableBitmap(tmp);
                    }
                }
            }
            
            return new LoadRequestResult<Bitmap>(RequestResult.ResultCodeOk, bitmap);
        }
    }
}