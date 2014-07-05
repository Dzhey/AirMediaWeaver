using AirMedia.Core.Log;

namespace AirMedia.Core
{
    public static class Consts
    {
        public const string HttpUserAgent = "AirMediaWeaver/1.0";
        public const int HttpStreamFileReadBufferSize = 1024*16;

        public const int DefaultWebClientTimeout = 5000;

        public const string UriPublicationsFragment = "publications";
        public const string UriTracksFragment = "tracks";
        public const string UriPeersFragment = "peers";
        public const string UriPeersUpdateFragment = "update";

        public const int SendMulticastAuthIntervalMillis = 10000;
        public const string DefaultMulticastAddress = "224.100.127.224";
        public const int DefaultMulticastPort = 6114;
        public const int DefaultHttpPort = 6116;
        public const int DefaultMulticastTTL = 3;
        public const int DefaultProgressDelayMillis = 500;

#if DEBUG
        public const bool Debug = true;
#else
        public const bool Debug = false;
#endif

        #region In-App Logging Settings

        public const int MaxInAppLoggingCacheEntries = 20000;
        public const int InApplogCleanThreshold = 100;

#if DEBUG
        /// <summary>
        /// Enable or disable in-app logging totally
        /// </summary>
        public const bool IsInAppLoggingEnabled = true;

        /// <summary>
        /// Set global log level
        /// </summary>
        public const LogLevel TmLogLevel = LogLevel.Verbose;

        /// <summary>
        /// Enable or disable default log panel settings appearance
        /// When set to 'false' one should perform several 
        /// taps on settings screen header to reveal settings.
        /// </summary>
        public const bool IsLogPanelSettingsRevealedByDefault = true;

        /// <summary>
        /// Enable or disable default log panel appearance
        /// </summary>
        public const bool IsLogPanelEnabledByDefault = true;
        public const bool IsLogEntriesListExpandedByDefault = true;
#else
        public const bool IsInAppLoggingEnabled = true;
        public const LogLevel TmLogLevel = LogLevel.Info;
        public const bool IsLogPanelSettingsRevealedByDefault = false;
        public const bool IsLogPanelEnabledByDefault = false;
        public const bool IsLogEntriesListExpandedByDefault = false;
#endif
        #endregion
    }
}