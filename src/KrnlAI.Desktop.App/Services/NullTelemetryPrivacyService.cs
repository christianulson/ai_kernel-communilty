using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

public sealed class NullTelemetryPrivacyService : ITelemetryPrivacyService
{
    private static readonly TelemetryPrivacyState State = new(
        TelemetryConsentLevel.None,
        "Não coletar",
        "A telemetria está desabilitada no modo local.",
        null,
        null);

    public Task<TelemetryPrivacyState> GetConsentAsync(CancellationToken ct = default) => Task.FromResult(State);

    public Task<TelemetryPrivacyState> SetConsentAsync(TelemetryConsentLevel consentLevel, CancellationToken ct = default)
        => Task.FromResult(State with { ConsentLevel = consentLevel });

    public Task<TelemetryPrivacyActionResult> RequestExportAsync(CancellationToken ct = default)
        => Task.FromResult(new TelemetryPrivacyActionResult(true, "Telemetria indisponível no modo local.", null));

    public Task<TelemetryPrivacyActionResult> RequestDeletionAsync(CancellationToken ct = default)
        => Task.FromResult(new TelemetryPrivacyActionResult(true, "Telemetria indisponível no modo local.", null));
}
