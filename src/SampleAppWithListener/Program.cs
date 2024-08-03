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
            var end = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, evt) => {
                cts.Cancel();
                end.WaitOne();
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
            end.Set();
        }
    }
}
