using RzWork.AzureMonitor;
using System.Collections.Specialized;

namespace TraceListenerTest;

public class ConfigTest
{
    [Fact]
    public void EmptyConfigFromEnv()
    {
        var config = Config.FromEnvironmentVars();
        Assert.False(config.Complete);
    }

    [Fact]
    public void EmptyConfigFromIMDS()
    {
        var config = Config.FromIMDS();
        Assert.False(config.Complete);
    }

    [Fact]
    void EmptyConfigFromAttributes()
    {
        var attrs = new StringDictionary();
        var config = Config.FromAttributes(attrs);
        Assert.False(config.Complete);
    }

    [Fact]
    void GetConfigFromAttributes()
    {
        var attrs = new StringDictionary();
        attrs[Config.MiClientIdKey] = "mi";
        attrs[Config.DceUrlKey] = "dce";
        attrs[Config.DcrIdKey] = "id";
        attrs[Config.DcrStreamKey] = "stream";
        var config = Config.FromAttributes(attrs);
        Assert.True(config.Complete);
        Assert.Equal("mi", config.MiClientId);
        Assert.Equal("dce", config.DceUrl);
        Assert.Equal("id", config.DcrId);
        Assert.Equal("stream", config.DcrStream);
    }

    [Fact]
    void GetMergedConfig()
    {
        var attrs = new StringDictionary();
        attrs[Config.MiClientIdKey] = "mi";
        attrs[Config.DceUrlKey] = "dce";
        attrs[Config.DcrIdKey] = "id";
        attrs[Config.DcrStreamKey] = "stream";
        var config = Config.Get(attrs);
        Assert.True(config.Complete);
        Assert.Equal("mi", config.MiClientId);
        Assert.Equal("dce", config.DceUrl);
        Assert.Equal("id", config.DcrId);
        Assert.Equal("stream", config.DcrStream);
    }
}
