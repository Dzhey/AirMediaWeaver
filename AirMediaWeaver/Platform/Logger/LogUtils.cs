using AirMedia.Core.Log;
using Android.Graphics;

namespace AirMedia.Platform.Logger
{
    public static class LogUtils
    {
        public static Color CreateColorFromResource(int colorResId)
        {
            int colorRaw = App.Instance.Resources.GetColor(colorResId);
            int red = Color.GetRedComponent(colorRaw);
            int green = Color.GetGreenComponent(colorRaw);
            int blue = Color.GetBlueComponent(colorRaw);

            return Color.Rgb(red, green, blue);
        }

        public static Color GetLogColor(LogLevel level)
        {
            return CreateColorFromResource(GetLogColorResId(level));
        }

        public static int GetLogColorResId(LogLevel level)
        {

            int colorId = Resource.Color.log_color_verbose;
            switch (level)
            {
                case LogLevel.Error:
                    colorId = Resource.Color.log_color_error;
                    break;

                case LogLevel.Warning:
                    colorId = Resource.Color.log_color_warning;
                    break;

                case LogLevel.Info:
                    colorId = Resource.Color.log_color_info;
                    break;

                case LogLevel.Debug:
                    colorId = Resource.Color.log_color_debug;
                    break;

                case LogLevel.Verbose:
                    colorId = Resource.Color.log_color_verbose;
                    break;
            }

            return colorId;
        }
    }
}