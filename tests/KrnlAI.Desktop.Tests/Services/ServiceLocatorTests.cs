using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public class ServiceLocatorTests
{
    [Fact]
    public void ServiceLocator_CreateLocalEmbeddedKernelOptions_ShouldUseSqliteStores()
    {
        var options = ServiceLocator.CreateLocalEmbeddedKernelOptions();

        Assert.Equal("Sqlite", options.StoreMode);
        Assert.Equal("Hybrid", options.SqliteMode);
        Assert.Equal("Sqlite", options.VectorMode);
        Assert.Equal("Memory", options.CacheMode);
    }
}
