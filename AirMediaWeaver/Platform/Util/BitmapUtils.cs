using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.Util
{
    public static class BitmapUtils
    {
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