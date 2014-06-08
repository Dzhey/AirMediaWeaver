using System;
using AirMedia.Core.Data;
using AirMedia.Core.Log;
using AirMedia.Platform.UI.Base;
using Android.Content;

namespace AirMedia.Platform.Data
{
    public class UserPreferences : CoreUserPreferences
    {
        private const string PreferencesName = "airmedia_prefs";

        private const string PreferenceClientGuid = "client_guid";
        private const string PreferenceLastMainView = "last_main_view";

        private const string PreferenceDatabaseCreated = "database_created";
        private const string PreferenceDisplayLogLevel = "display_log_level";
        private const string PreferenceLastReadLogEntryId = "last_read_log_entry_id";
        private const string PreferenceIsLogPanelSettingsRevealed = "is_log_panel_settings_revealed";
        private const string PreferencIsLogListExpanded = "is_log_list_expanded";
        private const string PreferenceIsLogPanelEnabled = "is_log_panel_enabled";

        private new static readonly string LogTag = typeof (UserPreferences).Name;

        private readonly ISharedPreferences _prefs;

        public bool IsLogListExpanded
        {
            get { return _prefs.GetBoolean(PreferencIsLogListExpanded, Core.Consts.IsLogEntriesListExpandedByDefault); }
            set { _prefs.Edit().PutBoolean(PreferencIsLogListExpanded, value).Commit(); }
        }

        public override bool IsLogPanelEnabled
        {
            get
            {
                if (Core.Consts.IsInAppLoggingEnabled == false) return false;

                return _prefs.GetBoolean(PreferenceIsLogPanelEnabled, Core.Consts.IsLogPanelEnabledByDefault);
            }
            set { _prefs.Edit().PutBoolean(PreferenceIsLogPanelEnabled, value).Commit(); }
        }

        public override bool IsLogPanelSettingsRevealed
        {
            get
            {
                if (Core.Consts.IsInAppLoggingEnabled == false) return false;

                return _prefs.GetBoolean(PreferenceIsLogPanelSettingsRevealed,
                                         Core.Consts.IsLogPanelSettingsRevealedByDefault);
            }
            set { _prefs.Edit().PutBoolean(PreferenceIsLogPanelSettingsRevealed, value).Commit(); }
        }

        public override bool DatabaseCreated
        {
            get { return _prefs.GetBoolean(PreferenceDatabaseCreated, false); }
            set { _prefs.Edit().PutBoolean(PreferenceDatabaseCreated, value).Commit(); }
        }

        public UserPreferences(Context context)
        {
            _prefs = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        }

        public LogLevel DisplayLogLevel
        {
            get
            {
                try
                {
                    int level = _prefs.GetInt(PreferenceDisplayLogLevel, (int) Core.Consts.TmLogLevel);

                    return (LogLevel) level;
                }
                catch (InvalidCastException e)
                {
                    AmwLog.Error(LogTag, e, "can't decode stored log level");
                }

                return LogLevel.Verbose;
            }

            set { _prefs.Edit().PutInt(PreferenceDisplayLogLevel, (int) value).Commit(); }
        }

        public override int LastReadLogEntryId
        {
            get { return _prefs.GetInt(PreferenceLastReadLogEntryId, 0); }

            set { _prefs.Edit().PutInt(PreferenceLastReadLogEntryId, value).Commit(); }
        }

        public override string ClientGuid
        {
            get
            {
                string guid = _prefs.GetString(PreferenceClientGuid, null);
                if (guid == null)
                {
                    guid = Guid.NewGuid().ToString();
                    ClientGuid = guid;
                }

                return guid;
            }

            set
            {
                if (value == null)
                {
                    AmwLog.Warn(LogTag, "client guid reset");
                }

                _prefs.Edit().PutString(PreferenceClientGuid, value).Commit();
            }
        }

        public Type LastMainView
        {
            get
            {
                string typeName = _prefs.GetString(PreferenceLastMainView, null);

                if (typeName == null) return null;

                var type = Type.GetType(typeName, false);

                if (type == null)
                {
                    AmwLog.Error(LogTag, "Can't retrieve main view type from preference; type name: \"{0}\"", typeName);
                    return null;
                }

                if (typeof (MainViewFragment).IsAssignableFrom(type) == false)
                {
                    AmwLog.Error(LogTag, "unexpected main view type from preference; type: \"{0}\"", type);

                    return null;
                }

                return type;
            }

            set
            {
                if (typeof (MainViewFragment).IsAssignableFrom(value) == false)
                {
                    throw new ArgumentException(string.Format(
                        "invalid main view type provided; type: \"{0}\"", value));
                }

                _prefs.Edit().PutString(PreferenceLastMainView, value.FullName).Commit();
            }
        }
    }
}