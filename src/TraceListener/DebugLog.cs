using System;
using System.Diagnostics;
using System.IO;

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

        private static string _EtwLog = typeof(DebugLog).FullName;

        private static bool _Verbose = false;

        private static string _ProcessName = null;

        private static int _ProcessId = 0;

        private static TextWriter _Out = Console.Error;

        private static bool _OutToFile = false;

        private static bool _EnableOut = false;

        static DebugLog()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                _ProcessName = currentProcess.ProcessName;
                _ProcessId = currentProcess.Id;
            }

            var outValue = Environment.GetEnvironmentVariable("DebugLog_Out");
            bool.TryParse(outValue, out _EnableOut);

            var verbValue = Environment.GetEnvironmentVariable("DebugLog_Verbose");
            bool.TryParse(verbValue, out _Verbose);

            var outFile = Environment.GetEnvironmentVariable("DebugLog_OutFile");
            if (!string.IsNullOrEmpty(outFile))
            {
                SetLogFile(outFile);
            }

            try
            {
                if (!EventLog.SourceExists(_EtwSource))
                {
                    EventLog.CreateEventSource(_EtwSource, _EtwLog);
                }
            }
            catch (Exception ex)
            {
                WriteOut(LogLevel.Warn, typeof(DebugLog).FullName, $"Error when accessing ETW: {ex}");
            }
        }

        private static void SetLogFile(string filename)
        {
            if (_OutToFile)
            {
                _Out.Close();
            }
            try
            {
                var fullname = $"{filename}_{_ProcessId}.txt";
                _Out = TextWriter.Synchronized(new StreamWriter(fullname, true));
                _OutToFile = true;
            }
            catch (Exception ex)
            {
                _Out = Console.Error;
                _OutToFile = false;
                WriteOut(LogLevel.Warn, typeof(DebugLog).FullName, $"Error when setting log filename: {ex}");
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
            WriteOut(level, category, msg);
            WriteEtw(level, category, msg);
        }

        private static void WriteOut(LogLevel level, string category, string msg)
        {
            if (_EnableOut)
            {
                try
                {
                    _Out.WriteLine($"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}] [{level}] [{_ProcessName}] [{_ProcessId}] [{category}] {msg}");
                    _Out.Flush();
                }
                catch (Exception) { }
            }
        }

        private static void WriteEtw(LogLevel level, string category, string msg)
        {
            try
            {
                if (EventLog.SourceExists(_EtwSource))
                {
                    EventLog.WriteEntry(_EtwSource, $"[{_ProcessName}] [{_ProcessId}] [{category}] {msg}", ToEventLogEntryType(level));
                }
            }
            catch (Exception) { }
        }
    }
}
