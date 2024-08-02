using System;
using System.Diagnostics;

namespace RzWork.AzureMonitor
{
    internal class Event
    {
        private static string _ComputerName = Environment.MachineName;

        private static string _ProcessName = null;

        private static int _ProcessId = 0;

        static Event()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                _ProcessName = currentProcess.ProcessName;
                _ProcessId = currentProcess.Id;
            }
        }

        public DateTime Time;

        public string ComputerName = _ComputerName;

        public string ProcessName = _ProcessName;

        public int ProcessId = _ProcessId;

        public TraceEventType Type;

        public int Id;

        public string Source;

        public string Content;

        public Event(DateTime time, TraceEventType type, int id, string source, string content)
        {
            Time = time;
            Type = type;
            Id = id;
            Source = source;
            Content = content;
        }
    }

}
