using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;

namespace AirMedia.Core.Log
{
    public abstract class AmwLog
    {
        protected struct LogEntry
        {
            public DateTime LogDate { get; set; }
            public LogLevel Level { get; set; }
            public string Tag { get; set; }
            public string DisplayMessage { get; set; }
            public string Details { get; set; }

            public override string ToString()
            {
                return string.Format("{0} {1}/{2}: {3}{4}", 
                    LogDate.ToString("dd/MM/yyyy HH:mm:ss.fff"),
                    Level,
                    Tag,
                    DisplayMessage,
                    Details == null ? "" : "\n" + Details);
            }
        }

        protected const int BulkInsertThreshold = 20;
        protected const int BulkInsertDelayMillis = 100;

        protected static AmwLog Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Mutex)
                    {
                        if (_instance == null)
                        {
                            throw new ApplicationException(string.Format(
                                "{0} instance is not created", typeof(AmwLog).Name));
                        }
                    }
                }

                return _instance;
            }
        }

        private static readonly object Mutex = new object();

        private static AmwLog _instance;

        private readonly ConcurrentQueue<LogEntry> _loggerQueue;
        private readonly Timer _timer;

        private double _lastInsertMillis;

        public static LogLevel LogLevel { get; set; }

        public static void Init(AmwLog instance)
        {
            lock (Mutex)
            {
                if (_instance != null)
                {
                    throw new ApplicationException(string.Format(
                        "{0} instance already set", typeof(AmwLog).Name));
                }

                _instance = instance;
                LogLevel = Consts.TmLogLevel;
            }
        }

        protected AmwLog()
        {
            _loggerQueue = new ConcurrentQueue<LogEntry>();
            _timer = new Timer(BulkInsertDelayMillis);
            _timer.AutoReset = true;
            _timer.Elapsed += OnTimerElapsed;
        }

        public static void Error(string tag, string displayMessage, object details)
        {
            Error(tag, displayMessage, details == null ? null : details.ToString());
        }

        public static void Error(string tag, string displayMessage, string details = null)
        {
            Instance.DispatchLogRequest(LogLevel.Error, tag, displayMessage, details);
        }

        public static void Warn(string tag, string displayMessage, string details = null)
        {
            Instance.DispatchLogRequest(LogLevel.Warning, tag, displayMessage, details);
        }

        public static void Info(string tag, string displayMessage, string details = null)
        {
            Instance.DispatchLogRequest(LogLevel.Info, tag, displayMessage, details);
        }

        public static void Debug(string tag, string displayMessage, object details)
        {
            Debug(tag, displayMessage, details == null ? null : details.ToString());
        }

        public static void Debug(string tag, string displayMessage, string details = null)
        {
            Instance.DispatchLogRequest(LogLevel.Debug, tag, displayMessage, details);
        }

        public static void Verbose(string tag, string displayMessage, string details = null)
        {
            Instance.DispatchLogRequest(LogLevel.Verbose, tag, displayMessage, details);
        }

        protected abstract void PerformEntryLog(LogEntry entry);

        /// <summary>
        /// Called periodically to perform bulk entity persist.
        /// </summary>
        /// <param name="entries"></param>
        protected abstract void SaveEntries(IEnumerable<LogEntry> entries);

        protected virtual LogEntry CreateLogEntry(LogLevel level, string tag, string displayMessage, string details)
        {
            return new LogEntry
            {
                LogDate = DateTime.UtcNow,
                Level = level,
                Tag = tag,
                DisplayMessage = displayMessage,
                Details = details
            };
        }

        protected virtual void DispatchLogRequest(LogLevel level, string tag, string displayMessage, string details)
        {
            if (tag == null)
            {
                tag = "";
            }

            if (displayMessage == null)
            {
                displayMessage = "null";
            }

            if (level < LogLevel) return;

            var logEntry = CreateLogEntry(level, tag, displayMessage, details);

            _loggerQueue.Enqueue(logEntry);

            _lastInsertMillis = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;

            PerformEntryLog(logEntry);

            if (_loggerQueue.Count >= BulkInsertThreshold)
            {
                BeginInsert();
            }
            else
            {
                _timer.Start();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs args)
        {
            double millis = TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds;

            if (millis - _lastInsertMillis > BulkInsertDelayMillis)
            {
                BeginInsert();
            }
            else
            {
                _timer.Start();
            }
        }

        private void BeginInsert()
        {
            int count = _loggerQueue.Count;

            if (count < 1) return;

            var entries = new List<LogEntry>();

            LogEntry entry;
            while (_loggerQueue.TryDequeue(out entry))
            {
                entries.Add(entry);
            }

            SaveEntries(entries);
        }
    }
}
