namespace KrnlAI.Desktop.Core.Models;

public enum TelemetryConsentLevel
{
    None = 0,
    Anonymous = 1,
    Full = 2
}

public sealed record TelemetryPrivacyState(
    TelemetryConsentLevel ConsentLevel,
    string Title,
    string Description,
    DateTimeOffset? GrantedAt,
    DateTimeOffset? RevokedAt);

public sealed record TelemetryPrivacyActionResult(
    bool Accepted,
    string Message,
    string? RequestId);
