using System;
using System.Diagnostics;

namespace RzWork.AzureMonitor
{
    internal static class DebugLog
    {
        enum LogLevel
        {
            Verb,
            Info,
            Warn,
            Error,
        }

        private static EventLogEntryType ToEventLogEntryType(LogLevel level)
        {
            EventLogEntryType t = EventLogEntryType.Information;
            switch (level)
            {
                case LogLevel.Warn:
                    t = EventLogEntryType.Warning;
                    break;
                case LogLevel.Error:
                    t = EventLogEntryType.Error;
                    break;
            }
            return t;
        }

        private static string _EtwSource = typeof(DebugLog).Namespace;

        private static string _EtwLog = typeof(DebugLog).Name;

        private static bool _Verbose = false;

        private static string _ProcessName = null;

        private static int _ProcessId = 0;

        static DebugLog()
        {
            if (!EventLog.SourceExists(_EtwSource))
            {
                try
                {
                    EventLog.CreateEventSource(_EtwSource, _EtwLog);
                }
                catch (Exception ex)
                {
                    WriteConsole(LogLevel.Error, typeof(DebugLog).FullName, $"Error when creating Event Source: {ex}");
                }
            }

            var verbValue = Environment.GetEnvironmentVariable("DebugLog_Verbose");
            bool.TryParse(verbValue, out _Verbose);

            using (var currentProcess = Process.GetCurrentProcess())
            {
                _ProcessName = currentProcess.ProcessName;
                _ProcessId = currentProcess.Id;
            }
        }

        public static void WriteVerbose<T>(string format, params object[] objects)
        {
            if (_Verbose)
            {
                Write<T>(LogLevel.Verb, string.Format(format, objects));
            }
        }

        public static void WriteInfo<T>(string msg)
        {
            Write<T>(LogLevel.Info, msg);
        }

        public static void WriteWarning<T>(string msg)
        {
            Write<T>(LogLevel.Warn, msg);
        }

        public static void WriteError<T>(string msg)
        {
            Write<T>(LogLevel.Error, msg);
        }

        private static void Write<T>(LogLevel level, string msg)
        {
            var category = typeof(T).FullName;
            if (EventLog.SourceExists(_EtwSource))
            {
                EventLog.WriteEntry(_EtwSource, $"[{_ProcessName}] [{_ProcessId}] [{category}] {msg}", ToEventLogEntryType(level));
            }
            else
            {
                WriteConsole(level, category, msg);
            }
        }

        private static void WriteConsole(LogLevel level, string category, string msg)
        {
            Console.Error.WriteLine($"[{_EtwLog}] [{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] [{level}] [{_ProcessName}] [{_ProcessId}] [{category}] {msg}");
        }
    }
}
