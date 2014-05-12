using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Data.Sql;
using AirMedia.Core.Data.Sql.Dao;
using AirMedia.Core.Data.Sql.Model;
using AirMedia.Core.Utils.StringUtils;
using SQLite;

namespace AirMedia.Core.Log
{
    public class LogEntryDao : BaseDao<LogEntryRecord>
    {
        public int GetLogEntryCount()
        {
            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(
                this, DatabaseHelper.Instance.LogDatabasePath))
            {
                return holder.Connection.ExecuteScalar<int>(string.Format(
                    "select count(*) from {0}", LogEntryRecord.TableLogEntry));
            }
        }

        public int GetLogEntryCount(LogLevel level)
        {
            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(
                this, DatabaseHelper.Instance.LogDatabasePath))
            {
                return holder.Connection.ExecuteScalar<int>(string.Format(
                    "select count(*) from {0} where {1}>={2}", 
                    LogEntryRecord.TableLogEntry, LogEntryRecord.ColumnLogLevel, (int) level));
            }
        }

        public int CountNewLogEntries(LogLevel level, int lastReadLogEntryId)
        {
            const string template = "select count(*) " +
                                    "from {tLogEntry} " +
                                    "where {tLogEntry}.{cId}>{lastReadEntryId} " +
                                    "and {cLogLevel}={logLevel}";

            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this,
                    DatabaseHelper.Instance.LogDatabasePath))
            {
                string query = template.HaackFormat(new
                    {
                        tLogEntry = LogEntryRecord.TableLogEntry,
                        cId = DatabaseRecord.ColumnId,
                        lastReadEntryId = lastReadLogEntryId,
                        cLogLevel = LogEntryRecord.ColumnLogLevel,
                        logLevel = (int) level
                    });

                return holder.Connection.ExecuteScalar<int>(query);
            }
        }

        /// <summary>
        /// Retrieve reversed list of log entries for specified log level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public List<LogEntryRecord> QueryForLogLevel(LogLevel level)
        {
            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this,
                    DatabaseHelper.Instance.LogDatabasePath))
            {
                return holder.Connection.Table<LogEntryRecord>()
                             .Where(record => record.Level >= level)
                             .Reverse()
                             .ToList();
            }
        }

        public int GetEntryCount()
        {
            using (var holder = DatabaseHelper.Instance.GetConnectionHolder(this,
                    DatabaseHelper.Instance.LogDatabasePath))
            {
                int result = holder.Connection.ExecuteScalar<int>(
                    "select count(*) from " + LogEntryRecord.TableLogEntry);

                return result;
            }
        }

        public int DeleteTopEntries(int rowCount, SQLiteConnection connection)
        {
            var items = connection.Table<LogEntryRecord>().Take(rowCount).ToArray();

            return DeleteAll(items, connection);
        }
    }
}
