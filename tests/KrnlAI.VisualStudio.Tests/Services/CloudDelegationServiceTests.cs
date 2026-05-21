using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class CloudDelegationServiceTests
{
    private sealed class MockSettings : ISettingsService
    {
        public string Endpoint { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string? DefaultProvider { get; set; }
        public string? DefaultModel { get; set; }
        public bool EnableInlineCompletions { get; set; }
        public bool EnableCodeLens { get; set; }
        public bool EnableHover { get; set; }
        public bool EnableCodeActions { get; set; }
        public global::KrnlAI.VisualStudio.Services.ApprovalMode ApprovalMode { get; set; }
        public bool EnableArtifactRendering { get; set; }
        public bool EnableStreaming { get; set; }
        public global::KrnlAI.VisualStudio.Services.CloudMode CloudMode { get; set; } = global::KrnlAI.VisualStudio.Services.CloudMode.Auto;
        public string? CloudEndpoint { get; set; }
        public bool EnableUsageTracking { get; set; }
        public void Load() { }
        public void Save() { }
    }

    [Fact]
    public void Mode_ShouldReturnSettingsValue()
    {
        var settings = new MockSettings { CloudMode = global::KrnlAI.VisualStudio.Services.CloudMode.AlwaysLocal };
        var service = new CloudDelegationService(settings);

        service.Mode.Should().Be(global::KrnlAI.VisualStudio.Services.CloudMode.AlwaysLocal);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_AlwaysLocal_ShouldRunLocal()
    {
        var settings = new MockSettings { CloudMode = global::KrnlAI.VisualStudio.Services.CloudMode.AlwaysLocal };
        var service = new CloudDelegationService(settings);

        var result = await service.ExecuteWithFallbackAsync(
            () => Task.FromResult("local"),
            () => Task.FromResult("cloud"));

        result.Should().Be("local");
        service.IsUsingCloud.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_AlwaysCloud_ShouldRunCloud()
    {
        var settings = new MockSettings { CloudMode = global::KrnlAI.VisualStudio.Services.CloudMode.AlwaysCloud };
        var service = new CloudDelegationService(settings);

        var result = await service.ExecuteWithFallbackAsync(
            () => Task.FromResult("local"),
            () => Task.FromResult("cloud"));

        result.Should().Be("cloud");
        service.IsUsingCloud.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_AutoMode_ShouldRunLocalOnFirstCall()
    {
        var settings = new MockSettings { CloudMode = global::KrnlAI.VisualStudio.Services.CloudMode.Auto };
        var service = new CloudDelegationService(settings);

        var result = await service.ExecuteWithFallbackAsync(
            () => Task.FromResult("local-result"),
            () => Task.FromResult("cloud-result"));

        result.Should().Be("local-result");
    }
}
