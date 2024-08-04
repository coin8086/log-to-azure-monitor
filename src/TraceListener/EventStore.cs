using Azure.Monitor.Ingestion;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RzWork.AzureMonitor
{
    internal class EventStore : IDisposable
    {
        private LogsIngestionClient _client;

        private string _dcrId;

        private string _dcrStream;

        private BlockingCollection<Event> _events = new BlockingCollection<Event>();

        private static int _checkDelay = 1000; //In MS

        private bool _disposed = false;

        public EventStore(LogsIngestionClient client, string dcrId, string dcrStream)
        {
            _client = client;
            _dcrId = dcrId;
            _dcrStream = dcrStream;
            Task.Run(ProcessEvents).ConfigureAwait(false);
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
            return count;
        }

        private async Task ProcessEvents()
        {
            var batchSize = 100;
            var batchBuf = new Event[batchSize];
            while (!_events.IsCompleted)
            {
                if (SendInBatch(batchSize, batchBuf) < batchSize)
                {
                    await Task.Delay(_checkDelay).ConfigureAwait(false);
                }
            }
        }

        public void Put(Event evt)
        {
            DebugLog.WriteVerbose<EventStore>("Put event: {0}", evt);
            _events.Add(evt);
        }

        //NOTE: Client should not keep writing trace log while Flushing, or the Flush may never end.
        public void Flush()
        {
            DebugLog.WriteInfo<EventStore>($"Events to flush: {_events.Count}");
            var batchSize = 100;
            var batchBuf = new Event[batchSize];
            while (_events.Count > 0)
            {
                SendInBatch(batchSize, batchBuf);
            }
        }

        public void Close()
        {
            DebugLog.WriteInfo<EventStore>($"Closing...");
            _events.CompleteAdding();
            Flush();
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
