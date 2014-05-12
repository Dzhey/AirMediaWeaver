using AirMedia.Core.Log;
using Android.Util;

namespace AirMedia.Platform.Logger
{
    public class AndroidAmwLog : AmwLogImpl
    {
        private static readonly string LogTag = typeof (AndroidAmwLog).Name;

        protected override void PerformEntryLog(LogEntry entry)
        {
            switch (entry.Level)
            {
                case LogLevel.Error:
                    Log.Error(entry.Tag, entry.DisplayMessage);
                    break;

                case LogLevel.Warning:
                    Log.Warn(entry.Tag, entry.DisplayMessage);
                    break;

                case LogLevel.Info:
                    Log.Info(entry.Tag, entry.DisplayMessage);
                    break;

                case LogLevel.Debug:
                    Log.Debug(entry.Tag, entry.DisplayMessage);
                    break;

                case LogLevel.Verbose:
                    Log.Verbose(entry.Tag, entry.DisplayMessage);
                    break;

                default:
                    Log.Wtf(LogTag, string.Format(
                        "Failed to dispatch log entry for LogLevel \"{0}\"", entry.Level));
                    break;
            }
        }

        public static void Info(string tag, int stringResId, string details = null)
        {
            string message = App.Instance.GetString(stringResId) ?? "<error fetching message>";
            AmwLog.Info(tag, message, details);
        }
    }
}