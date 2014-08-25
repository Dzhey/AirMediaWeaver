
using Android.Graphics;

namespace AirMedia.Platform.Util
{
    public interface IReuseBitmapCacheAccessor
    {
        void AddInBitmapOptions(BitmapFactory.Options options);
        void AddReusableBitmap(Bitmap bitmap);
    }
}