using RzWork.AzureMonitor;
using System.Diagnostics;

namespace TraceListenerTest;

public class EventStoreTest
{
    [Fact]
    public async Task LogsAreSentPeriodically()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        await Task.Delay(1100);
        Assert.Equal(9, sender.Count);
    }

    [Fact]
    public async Task LogsAreSentOnThreshold()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        eventStore.Put(evt);
        //Assert.True(sender.Count < 10);
        await Task.Delay(500);
        Assert.Equal(10, sender.Count);
    }

    [Fact]
    public async Task LogsAreSentOnImportantEvent()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 5; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        var warning = new Event(DateTime.UtcNow, TraceEventType.Warning, 0, "", "Some warning");
        eventStore.Put(warning);
        Assert.True(sender.Count < 6);
        await Task.Delay(500);
        Assert.Equal(6, sender.Count);
    }

    [Fact]
    public void SynchronousFlush()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        eventStore.Flush(true);
        Assert.Equal(9, sender.Count);
    }

    [Fact]
    public async Task ASynchronousFlush()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        eventStore.Flush(false);
        Assert.True(sender.Count < 9);
        await Task.Delay(500);
        Assert.Equal(9, sender.Count);
    }

    [Fact]
    public void LogsAreSentOnClose()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        eventStore.Close();
        Assert.Equal(9, sender.Count);
    }

    [Fact]
    public void UnavailableAfterClose()
    {
        var sender = new MockLogSender();
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        for (var i = 0; i < 9; i++)
        {
            eventStore.Put(evt);
        }
        Assert.Equal(0, sender.Count);
        eventStore.Close();
        Assert.Equal(9, sender.Count);
        eventStore.Put(evt);
        eventStore.Flush(true);
        Assert.Equal(9, sender.Count);
    }

    [Fact]
    public void NoExceptionWhenLogSenderThrows()
    {
        var sender = new MockLogSender("some exception");
        using var eventStore = new EventStore(sender, 1000, 10);
        var evt = new Event(DateTime.UtcNow, TraceEventType.Information, 0, "", "OK");
        eventStore.Put(evt);
        eventStore.Flush(true);
        Assert.Equal(0, sender.Count);
    }
}
