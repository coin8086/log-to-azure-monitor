using System;
using System.Diagnostics;

namespace RzWork.AzureMonitor
{
    internal static class DebugLog
    {
        enum LogLevel
        {
            Info,
            Warning,
            Error,
        }

        private static EventLogEntryType ToEventLogEntryType(LogLevel level)
        {
            EventLogEntryType t = EventLogEntryType.Information;
            switch (level)
            {
                case LogLevel.Warning:
                    t = EventLogEntryType.Warning;
                    break;
                case LogLevel.Error:
                    t = EventLogEntryType.Error;
                    break;
            }
            return t;
        }

        private static string EtwSource = typeof(DebugLog).Namespace;

        private static string EtwLog = typeof(DebugLog).FullName;

        static DebugLog()
        {
            if (!EventLog.Exists(EtwSource))
            {
                try
                {
                    EventLog.CreateEventSource(EtwSource, EtwLog);
                }
                catch (Exception ex)
                {
                    WriteConsole(LogLevel.Error, typeof(DebugLog).FullName, $"Error when creating Event Source: {ex}");
                }
            }
        }

        public static void WriteInfo<T>(string msg)
        {
            Write<T>(LogLevel.Info, msg);
        }

        public static void WriteWarning<T>(string msg)
        {
            Write<T>(LogLevel.Warning, msg);
        }

        public static void WriteError<T>(string msg)
        {
            Write<T>(LogLevel.Error, msg);
        }

        private static void Write<T>(LogLevel level, string msg)
        {
            var category = typeof(T).FullName;
            if (EventLog.Exists(EtwSource))
            {
                EventLog.WriteEntry(EtwSource, $"[{category}] {msg}", ToEventLogEntryType(level));
            }
            else
            {
                WriteConsole(level, category, msg);
            }
        }

        private static void WriteConsole(LogLevel level, string category, string msg)
        {
            Console.Error.WriteLine($"[{EtwLog}] [{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] [{level}] [{category}] {msg}");
        }
    }
}
