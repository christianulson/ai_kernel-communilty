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
    public void Constructor_LocalMode_ShouldBeLocal()
    {
        var factory = new ModeProviderFactory(true);
        Assert.True(factory.IsLocal);
    }

    [Fact]
    public void Constructor_ApiMode_ShouldNotBeLocal()
    {
        var factory = new ModeProviderFactory(false);
        Assert.False(factory.IsLocal);
    }
}
