using RzWork.AzureMonitor;

namespace TraceListenerTest;

internal class MockLogSender : ILogSender
{
    private string? _exception;

    private int _count = 0;

    public int Count => _count;

    public MockLogSender(string? exception = null)
    {
        _exception = exception;
    }

    public void Send<T>(IEnumerable<T> logs)
    {
        if (!string.IsNullOrEmpty(_exception))
        {
            throw new ApplicationException(_exception);
        }
        else
        {
            var count = logs.Count<T>();
            Interlocked.Add(ref _count, count);
        }
    }
}
