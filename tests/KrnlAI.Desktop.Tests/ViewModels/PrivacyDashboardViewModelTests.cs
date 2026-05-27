using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class PrivacyDashboardViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldPopulateConsentAndDescription()
    {
        var service = new FakeTelemetryPrivacyService(
            new TelemetryPrivacyState(TelemetryConsentLevel.Anonymous, "Anônima", "Coleta anônima", new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero), null));

        var vm = new PrivacyDashboardViewModel(service);

        await vm.LoadAsync(CancellationToken.None);

        Assert.Equal(TelemetryConsentLevel.Anonymous, vm.SelectedConsentLevel);
        Assert.Contains("anônima", vm.ConsentDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("carregado", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestExportAsync_ShouldUpdateStatusMessage()
    {
        var service = new FakeTelemetryPrivacyService(
            new TelemetryPrivacyState(TelemetryConsentLevel.Full, "Completa", "Coleta completa", DateTimeOffset.UtcNow, null))
        {
            ExportResult = new TelemetryPrivacyActionResult(true, "export accepted", "req-999")
        };

        var vm = new PrivacyDashboardViewModel(service);

        await vm.RequestExportAsync(CancellationToken.None);

        Assert.Equal("export accepted", vm.StatusMessage);
        Assert.Equal("req-999", service.LastExportRequestId);
    }

    private sealed class FakeTelemetryPrivacyService : ITelemetryPrivacyService
    {
        private readonly TelemetryPrivacyState _state;

        public FakeTelemetryPrivacyService(TelemetryPrivacyState state) => _state = state;

        public TelemetryPrivacyActionResult? ExportResult { get; init; }
        public TelemetryPrivacyActionResult? DeleteResult { get; init; }
        public string? LastExportRequestId { get; private set; }

        public Task<TelemetryPrivacyState> GetConsentAsync(CancellationToken ct = default)
            => Task.FromResult(_state);

        public Task<TelemetryPrivacyState> SetConsentAsync(TelemetryConsentLevel level, CancellationToken ct = default)
            => Task.FromResult(_state with { ConsentLevel = level });

        public Task<TelemetryPrivacyActionResult> RequestExportAsync(CancellationToken ct = default)
        {
            LastExportRequestId = ExportResult?.RequestId;
            return Task.FromResult(ExportResult ?? new TelemetryPrivacyActionResult(true, "ok", null));
        }

        public Task<TelemetryPrivacyActionResult> RequestDeletionAsync(CancellationToken ct = default)
            => Task.FromResult(DeleteResult ?? new TelemetryPrivacyActionResult(true, "ok", null));
    }
}
