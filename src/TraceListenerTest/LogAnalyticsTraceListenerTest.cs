using RzWork.AzureMonitor;
using System.Diagnostics;

namespace TraceListenerTest;

public class LogAnalyticsTraceListenerTest
{
    [Fact]
    public void NullParams()
    {
        using var listener = new LogAnalyticsTraceListener();
        listener.Write(null);
        listener.WriteLine(null);
        listener.Fail(null);
        listener.Fail(null, null);
        listener.TraceEvent(null, null, TraceEventType.Information, 0);
        listener.TraceEvent(null, null, TraceEventType.Information, 0, null);
        listener.TraceEvent(null, null, TraceEventType.Information, 0, null, null);
        listener.TraceData(null, null, TraceEventType.Information, 0, null);
        listener.TraceData(null, null, TraceEventType.Information, 0, null, null);
    }

    [Fact]
    public void PassWithoutConfig()
    {
        using var listener = new LogAnalyticsTraceListener();
        listener.WriteLine("Hello");
        listener.Close();
    }

    [Fact]
    public void NoExceptionWithWrongConfig()
    {
        using var listener = new LogAnalyticsTraceListener();
        listener.Attributes[Config.MiClientIdKey] = "some value";
        listener.Attributes[Config.DceUrlKey] = "some value";
        listener.Attributes[Config.DcrIdKey] = "some value";
        listener.Attributes[Config.DcrStreamKey] = "some value";
        listener.WriteLine("Hello");
        listener.Close();
    }

    [Fact]
    public void NoExceptionWithIncompleteConfig()
    {
        using var listener = new LogAnalyticsTraceListener();
        listener.Attributes[Config.MiClientIdKey] = "some value";
        listener.WriteLine("Hello");
        listener.Close();
    }
}
