using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SampleAppWithListener
{
    class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            Console.CancelKeyPress += (sender, evt) => {
                //NOTE: Do not exit process in this callback, or the TraceListeners' Close() method
                //won't get called.
                evt.Cancel = true;
                cts.Cancel();
            };

            Console.WriteLine("Main starts.");

            while (!token.IsCancellationRequested)
            {
                Trace.WriteLine("some line");
                Trace.TraceInformation("some info");
                Trace.TraceWarning("some warning");
                Trace.TraceError("some error");

                try
                {
                    Task.Delay(2000).Wait(token);
                }
                catch (OperationCanceledException) { }
            }

            Console.WriteLine("Main ends.");
        }
    }
}
