﻿using System;
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

        public DateTime Time { get; set; }

        public string ComputerName { get; set; } = _ComputerName;

        public string ProcessName { get; set; } = _ProcessName;

        public int ProcessId { get; set; } = _ProcessId;

        //NOTE: Avoid the name "Type" since it's a reserved table column name in Azure Log Analytics.
        public string EventType { get; set; }

        public int Id { get; set; }

        public string Source { get; set; }

        public string Content { get; set; }

        public Event(DateTime time, TraceEventType type, int id, string source, string content)
        {
            Time = time;
            EventType = type.ToString();
            Id = id;
            Source = source;
            Content = content;
        }
    }

}
