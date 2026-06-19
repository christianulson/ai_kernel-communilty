using AutoFixture;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class PrivacyDashboardViewModelTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public async Task LoadAsync_ShouldPopulateConsentAndDescription()
    {
        var service = new Mock<ITelemetryPrivacyService>();
        service.Setup(x => x.GetConsentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelemetryPrivacyState(TelemetryConsentLevel.Anonymous, "Anônima", "Coleta anônima",
                new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero), null));

        var vm = new PrivacyDashboardViewModel(service.Object);

        await vm.LoadAsync(CancellationToken.None);

        Assert.Equal(TelemetryConsentLevel.Anonymous, vm.SelectedConsentLevel);
        Assert.Contains("anônima", vm.ConsentDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("carregado", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestExportAsync_ShouldUpdateStatusMessage()
    {
        var service = new Mock<ITelemetryPrivacyService>();
        service.Setup(x => x.GetConsentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelemetryPrivacyState(TelemetryConsentLevel.Full, "Completa", "Coleta completa",
                DateTimeOffset.UtcNow, null));
        service.Setup(x => x.RequestExportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelemetryPrivacyActionResult(true, "export accepted", "req-999"));

        var vm = new PrivacyDashboardViewModel(service.Object);

        await vm.RequestExportAsync(CancellationToken.None);

        Assert.Equal("export accepted", vm.StatusMessage);
    }
}
