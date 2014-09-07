
using Android.Graphics;

namespace AirMedia.Platform.Util
{
    public interface IReuseBitmapCacheAccessor
    {
        bool AddInBitmapOptions(BitmapFactory.Options options);
        void AddReusableBitmap(Bitmap bitmap);
    }
}