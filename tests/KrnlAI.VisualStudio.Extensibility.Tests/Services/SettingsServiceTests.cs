using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Load_WithMemoryStore_ShouldRestoreSavedValues()
    {
        var store = new MemorySettingsStore();
        var settings = new SettingsService(store);
        settings.Endpoint = "http://localhost:6000";
        settings.SidecarPort = 5111;
        settings.EnableStreaming = false;
        settings.Save();

        var reloaded = new SettingsService(store);
        reloaded.Load();

        reloaded.Endpoint.Should().Be("http://localhost:6000");
        reloaded.SidecarPort.Should().Be(5111);
        reloaded.EnableStreaming.Should().BeFalse();
    }

    [Fact]
    public void Load_EmptyStore_ShouldKeepDefaults()
    {
        var settings = new SettingsService(new MemorySettingsStore());
        settings.Load();

        settings.Endpoint.Should().Be("http://localhost:5235");
        settings.RuntimeMode.Should().Be(KernelRuntimeMode.LocalApi);
        settings.SidecarPort.Should().Be(5001);
    }
}