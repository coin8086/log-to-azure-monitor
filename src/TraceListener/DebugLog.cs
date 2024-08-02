using System;

namespace RzWork.AzureMonitor
{
    internal static class DebugLog
    {
        public static void WriteInfo<T>(string msg)
        {
            Write("Info", typeof(T).FullName, msg);
        }

        public static void WriteWarning<T>(string msg)
        {
            Write("Warn", typeof(T).FullName, msg);
        }

        public static void WriteError<T>(string msg)
        {
            Write("Error", typeof(T).FullName, msg);
        }

        private static void Write(string level, string category, string msg)
        {
            Console.Error.WriteLine($"LA {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {level} {category} {msg}");
        }
    }
}
