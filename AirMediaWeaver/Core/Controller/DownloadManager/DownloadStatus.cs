

namespace AirMedia.Core.Controller.DownloadManager
{
    public static class DownloadStatus
    {
        public const int Failed = -2;
        public const int Unknown = -1;
        public const int Pending = 1;
        public const int Running = 2;
        public const int Paused = 3;
        public const int Successful = 4;

        public static string GetString(int downloadStatus)
        {
            switch (downloadStatus)
            {
                case Failed:
                    return "failed";

                case Unknown:
                    return "unknown";

                case Pending:
                    return "pending";

                case Running:
                    return "running";

                case Paused:
                    return "paused";

                case Successful:
                    return "successful";

                default:
                    return "undefined status";
            }
        }
    }
}