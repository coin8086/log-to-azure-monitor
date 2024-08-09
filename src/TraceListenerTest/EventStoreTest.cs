using RzWork.AzureMonitor;
using System.Diagnostics;

namespace TraceListenerTest;

public class EventStoreTest : IDisposable
{
    private MockLogSender _sender;

    private EventStore _eventStore;

    private Event _evt;

    private const int FlushInterval = 1000;

    private const int FlushThreshold = 10;

    public EventStoreTest()
    {
        _sender = new MockLogSender();
        _eventStore = new EventStore(_sender, FlushInterval, FlushThreshold);
        _evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
    }

    public void Dispose()
    {
        _eventStore.Dispose();
    }


    [Fact]
    public async Task LogsAreSentPeriodically()
    {
        var count = FlushThreshold - 1;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        await Task.Delay((int)(FlushInterval * 1.1));
        Assert.Equal(count, _sender.Count);
    }

    [Fact]
    public async Task LogsAreSentOnThreshold()
    {
        var count = FlushThreshold - 1;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        _eventStore.Put(_evt);
        await Task.Delay(FlushInterval / 2);
        Assert.Equal(FlushThreshold, _sender.Count);
    }

    [Fact]
    public async Task LogsAreSentOnImportantEvent()
    {
        _eventStore.Put(_evt);
        Assert.Equal(0, _sender.Count);

        var error = new Event(DateTime.UtcNow, TraceEventType.Error, 0, "", "Some error");
        _eventStore.Put(error);

        await Task.Delay(FlushInterval / 2);
        Assert.Equal(2, _sender.Count);
    }

    [Fact]
    public void SynchronousFlush()
    {
        var count = FlushThreshold / 2;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        _eventStore.Flush(true);
        Assert.Equal(count, _sender.Count);
    }

    [Fact]
    public async Task ASynchronousFlush()
    {
        var count = FlushThreshold / 2;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        _eventStore.Flush(false);
        Assert.True(_sender.Count < count);

        await Task.Delay(FlushInterval / 2);
        Assert.Equal(count, _sender.Count);
    }

    [Fact]
    public void LogsAreSentOnClose()
    {
        var count = FlushThreshold - 1;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        _eventStore.Close();
        Assert.Equal(count, _sender.Count);
    }

    [Fact]
    public void CannotPutMoreAfterClose()
    {
        var count = FlushThreshold - 1;
        for (var i = 0; i < count; i++)
        {
            _eventStore.Put(_evt);
        }
        Assert.Equal(0, _sender.Count);

        _eventStore.Close();
        Assert.Equal(count, _sender.Count);

        _eventStore.Put(_evt);
        _eventStore.Flush(true);
        Assert.Equal(count, _sender.Count);
    }

    [Fact]
    public void NoExceptionWhenLogSenderThrows()
    {
        var sender = new MockLogSender("some exception");
        using var eventStore = new EventStore(sender, FlushInterval, FlushThreshold);
        eventStore.Put(_evt);
        eventStore.Flush(true);
        Assert.Equal(0, sender.Count);
    }
}
