using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class ApprovalServiceTests
{
    private sealed class MockSettingsService : ISettingsService
    {
        public string Endpoint { get; set; } = "http://localhost:65335";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string? DefaultProvider { get; set; }
        public string? DefaultModel { get; set; }
        public bool EnableInlineCompletions { get; set; } = true;
        public bool EnableCodeLens { get; set; } = true;
        public bool EnableHover { get; set; } = true;
        public bool EnableCodeActions { get; set; } = true;
        public global::KrnlAI.VisualStudio.Services.ApprovalMode ApprovalMode { get; set; } = global::KrnlAI.VisualStudio.Services.ApprovalMode.Confirm;
        public bool EnableArtifactRendering { get; set; } = true;
        public bool EnableStreaming { get; set; } = true;
        public global::KrnlAI.VisualStudio.Services.CloudMode CloudMode { get; set; }
        public string? CloudEndpoint { get; set; }
        public bool EnableUsageTracking { get; set; }
        public void Load() { }
        public void Save() { }
    }

    [Fact]
    public void Mode_ShouldReturnSettingsValue()
    {
        var settings = new MockSettingsService { ApprovalMode = global::KrnlAI.VisualStudio.Services.ApprovalMode.ChatOnly };
        var service = new ApprovalService(settings);

        service.Mode.Should().Be(global::KrnlAI.VisualStudio.Services.ApprovalMode.ChatOnly);
    }

    [Fact]
    public void Mode_WhenFullApproval_ShouldReturnFullApproval()
    {
        var settings = new MockSettingsService { ApprovalMode = global::KrnlAI.VisualStudio.Services.ApprovalMode.FullApproval };
        var service = new ApprovalService(settings);

        service.Mode.Should().Be(global::KrnlAI.VisualStudio.Services.ApprovalMode.FullApproval);
    }

    [Fact]
    public async Task RequestApprovalAsync_ChatOnlyMode_ShouldReject()
    {
        var settings = new MockSettingsService { ApprovalMode = global::KrnlAI.VisualStudio.Services.ApprovalMode.ChatOnly };
        var service = new ApprovalService(settings);

        var result = await service.RequestApprovalAsync("test action", "details", RiskLevel.Low);

        result.Approved.Should().BeFalse();
        result.Comment.Should().Contain("Chat-only");
    }

    [Fact]
    public async Task RequestApprovalAsync_LowRiskConfirmMode_ShouldRequireApproval()
    {
        var settings = new MockSettingsService { ApprovalMode = global::KrnlAI.VisualStudio.Services.ApprovalMode.Confirm };
        var service = new ApprovalService(settings);

        var result = await service.RequestApprovalAsync("test", "", RiskLevel.Low);

        // No app context in tests -> dispatcher is null -> returns cancelled
        result.Approved.Should().BeFalse();
    }
}
