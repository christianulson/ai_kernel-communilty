using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface ITelemetryPrivacyService
{
    Task<TelemetryPrivacyState> GetConsentAsync(CancellationToken ct = default);
    Task<TelemetryPrivacyState> SetConsentAsync(TelemetryConsentLevel consentLevel, CancellationToken ct = default);
    Task<TelemetryPrivacyActionResult> RequestExportAsync(CancellationToken ct = default);
    Task<TelemetryPrivacyActionResult> RequestDeletionAsync(CancellationToken ct = default);
}
