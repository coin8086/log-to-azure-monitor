using System;
using System.Diagnostics;

namespace SampleAppWithListener
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Main starts.");

            Trace.WriteLine("some line");
            Trace.TraceInformation("some info");
            Trace.TraceWarning("some warning");
            Trace.TraceError("some error");

            Console.WriteLine("Main ends.");
        }
    }
}
