using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class ModeProviderFactoryTests
{
    [Fact]
    public void Constructor_Default_ShouldBeApiMode()
    {
        var factory = new ModeProviderFactory();
        Assert.False(factory.IsLocal);
    }

    [Fact]
    public void Constructor_LocalTrue_ShouldBeLocal()
    {
        var factory = new ModeProviderFactory(true);
        Assert.True(factory.IsLocal);
    }

    [Fact]
    public void Constructor_LocalFalse_ShouldBeApi()
    {
        var factory = new ModeProviderFactory(false);
        Assert.False(factory.IsLocal);
    }

    [Fact]
    public void Constructor_ExplicitTrue_ReturnsTrue()
    {
        var factory = new ModeProviderFactory(true);
        Assert.True(factory.IsLocal);
    }

    [Fact]
    public void Constructor_ImplicitFalse_ReturnsFalse()
    {
        var factory = new ModeProviderFactory();
        Assert.False(factory.IsLocal);
    }

    [Fact]
    public void MultipleInstances_EachHasOwnState()
    {
        var local = new ModeProviderFactory(true);
        var api = new ModeProviderFactory(false);
        Assert.True(local.IsLocal);
        Assert.False(api.IsLocal);
    }
}
