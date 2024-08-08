using Azure.Monitor.Ingestion;
using System.Collections.Generic;

namespace RzWork.AzureMonitor
{
    internal class LogAnalyticsSender : ILogSender
    {
        private LogsIngestionClient _client;

        private string _dcrId;

        private string _dcrStream;

        public LogAnalyticsSender(LogsIngestionClient client, string dcrId, string dcrStream)
        {
            _client = client;
            _dcrId = dcrId;
            _dcrStream = dcrStream;
        }

        public void Send<T>(IEnumerable<T> logs)
        {
            _client.Upload(_dcrId, _dcrStream, logs);
        }
    }
}
