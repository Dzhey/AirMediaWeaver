using System;

namespace AirMedia.Core.Data
{
    public abstract class CoreUserPreferences
    {
        public static string LogTag = typeof (CoreUserPreferences).Name;

        public static CoreUserPreferences Instance { get; private set; }

        public abstract bool DatabaseCreated { get; set; }
        public abstract bool IsLogPanelEnabled { get; set; }
        public abstract bool IsLogPanelSettingsRevealed { get; set; }
        public abstract int LastReadLogEntryId { get; set; }

        protected CoreUserPreferences()
        {
            Instance = this;
        }
    }
}
