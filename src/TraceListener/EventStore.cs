using Azure.Monitor.Ingestion;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using SystemTimer = System.Timers.Timer;

namespace RzWork.AzureMonitor
{
    internal class EventStore : IDisposable
    {
        private LogsIngestionClient _client;

        private string _dcrId;

        private string _dcrStream;

        private BlockingCollection<Event> _events = new BlockingCollection<Event>();

        private const int FlushInterval = 1000 * 60; //In MS

        private const int FlushThreshold = 100;

        //TODO: Dispose it?
        private EventWaitHandle _processingEvent = new ManualResetEvent(false);

        //TODO: Dispose it?
        private SystemTimer _flushTimer = new SystemTimer(FlushInterval);

        private bool _disposed = false;

        public EventStore(LogsIngestionClient client, string dcrId, string dcrStream)
        {
            _client = client;
            _dcrId = dcrId;
            _dcrStream = dcrStream;
            Task.Run(ProcessEvents).ConfigureAwait(false);
            _flushTimer.Elapsed += OnFlushTimerFired;
            _flushTimer.Start();
        }

        private void OnFlushTimerFired(object sender, System.Timers.ElapsedEventArgs e)
        {
            Flush(false);
        }

        private int PopulateBuf(Event[] buf, int bufSize)
        {
            int index = 0;
            Event evt = null;
            while (index < bufSize && _events.TryTake(out evt))
            {
                buf[index++] = evt;
            }
            return index;
        }

        private int SendInBatch(int batchSize, Event[] batchBuf)
        {
            Debug.Assert(batchBuf != null && batchBuf.Length >= batchSize);
            var count = PopulateBuf(batchBuf, batchSize);
            if (count > 0)
            {
                DebugLog.WriteVerbose<EventStore>("{0} events to send.", count);
                var logs = new ArraySegment<Event>(batchBuf, 0, count);
                try
                {
                    _client.Upload(_dcrId, _dcrStream, logs);
                }
                catch (Exception ex)
                {
                    DebugLog.WriteError<EventStore>($"Error on uploading logs: {ex}");
                    return 0;
                }
            }
            else
            {
                DebugLog.WriteVerbose<EventStore>("No event to send.");
            }
            return count;
        }

        private void ProcessEvents()
        {
            var batchSize = 100;
            var batchBuf = new Event[batchSize];
            while (!_events.IsCompleted)
            {
                if (SendInBatch(batchSize, batchBuf) == 0)
                {
                    _processingEvent.Reset();
                    _processingEvent.WaitOne();
                }
            }
        }

        public void Put(Event evt)
        {
            DebugLog.WriteVerbose<EventStore>("Put event: {0}", evt);
            _events.Add(evt);
            var count = _events.Count;
            if (count >= FlushThreshold)
            {
                DebugLog.WriteVerbose<EventStore>("Trigger a flush by {0} events", count);
                Flush(false);
            }
        }

        //NOTE: Client should not keep writing trace log while Flushing, or the Flush may never end.
        public void Flush(bool synchronized)
        {
            DebugLog.WriteInfo<EventStore>($"Events to flush: {_events.Count}");
            if (synchronized)
            {
                var batchSize = 100;
                var batchBuf = new Event[batchSize];
                while (_events.Count > 0)
                {
                    SendInBatch(batchSize, batchBuf);
                }
            }
            else
            {
                _processingEvent.Set();
            }
        }

        public void Close()
        {
            DebugLog.WriteInfo<EventStore>($"Closing...");
            _flushTimer.Stop();
            if (!_events.IsCompleted)
            {
                _events.CompleteAdding();
                Flush(true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
