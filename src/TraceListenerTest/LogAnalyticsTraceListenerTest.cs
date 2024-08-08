using RzWork.AzureMonitor;

namespace TraceListenerTest;

public class LogAnalyticsTraceListenerTest
{
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
