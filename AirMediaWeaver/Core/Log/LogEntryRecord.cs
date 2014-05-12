using System;
using AirMedia.Core.Data.Sql.Model;
using SQLite;

namespace AirMedia.Core.Log
{
    [Table(TableLogEntry)]
    public class LogEntryRecord : DatabaseRecord
    {
        public const string TableLogEntry = "T_LOG_ENTRY";
        public const string ColumnLogLevel = "C_LOG_LEVEL";

        [Column("C_DATE")]
        public DateTime Date { get; set; }

        [Column(ColumnLogLevel)]
        public LogLevel Level { get; set; }

        [Column("C_TAG")]
        public string Tag { get; set; }

        [Column("C_MESSAGE")]
        public string Message { get; set; }

        [Column("C_DETAILS")]
        public string Details { get; set; }

        public string ToLogEntry()
        {
            return string.Format("{0} {1}/{2}: {3}\n{4}",
                Date.ToString("dd/MM/yyyy HH:mm:ss.fff"),
                Level,
                Tag,
                Message,
                Details);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}/{2}: {3}",
                Date.ToString("dd/MM/yyyy HH:mm:ss.fff"),
                Level,
                Tag,
                Message);
        }
    }
}
