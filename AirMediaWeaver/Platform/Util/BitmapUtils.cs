using System;

using Android.Graphics;
using Android.OS;

namespace AirMedia.Platform.Util
{
    public static class BitmapUtils
    {
        public static bool CanUseForInBitmap(Bitmap candidate, BitmapFactory.Options targetOptions)
        {
            if (candidate.Handle == IntPtr.Zero)
                return false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                int width = targetOptions.OutWidth / targetOptions.InSampleSize;
                int height = targetOptions.OutHeight / targetOptions.InSampleSize;
                int byteCount = width * height * GetBytesPerPixel(candidate.GetConfig());

                return byteCount <= candidate.AllocationByteCount;
            }

            return candidate.Width == targetOptions.OutWidth
                    && candidate.Height == targetOptions.OutHeight
                    && targetOptions.InSampleSize == 1;
        }

        public static int GetBitmapSize(Bitmap bitmap)
        {
            if (bitmap.Handle == IntPtr.Zero)
            {
                return 0;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                return bitmap.AllocationByteCount;
            }

            return bitmap.ByteCount;
        }

        public static int GetBytesPerPixel(Bitmap.Config config)
        {
            if (config == Bitmap.Config.Argb8888)
            {
                return 4;
            }

            if (config == Bitmap.Config.Rgb565)
            {
                return 2;
            }

            if (config == Bitmap.Config.Argb4444)
            {
                return 2;
            }

            if (config == Bitmap.Config.Alpha8)
            {
                return 1;
            }

            return 1;
        }
    }
}