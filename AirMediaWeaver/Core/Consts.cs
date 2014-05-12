using AirMedia.Core.Log;

namespace AirMedia.Core
{
    public static class Consts
    {
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