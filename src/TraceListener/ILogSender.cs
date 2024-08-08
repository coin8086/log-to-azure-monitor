using System.Collections.Generic;

namespace RzWork.AzureMonitor
{
    public interface ILogSender
    {
        void Send<T>(IEnumerable<T> logs);
    }
}
