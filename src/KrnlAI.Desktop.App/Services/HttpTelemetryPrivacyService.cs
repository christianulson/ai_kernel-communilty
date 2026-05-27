using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.Services;

public sealed class HttpTelemetryPrivacyService(HttpClient httpClient) : ITelemetryPrivacyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TelemetryPrivacyState> GetConsentAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/api/privacy/telemetry/consent", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ConsentDto>(JsonOptions, ct).ConfigureAwait(false);
        return MapState(dto);
    }

    public async Task<TelemetryPrivacyState> SetConsentAsync(TelemetryConsentLevel consentLevel, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("/api/privacy/telemetry/consent",
            new { consentLevel = consentLevel.ToString() }, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ConsentDto>(JsonOptions, ct).ConfigureAwait(false);
        return MapState(dto);
    }

    public Task<TelemetryPrivacyActionResult> RequestExportAsync(CancellationToken ct = default)
        => RequestActionAsync("/api/privacy/telemetry/export", ct);

    public Task<TelemetryPrivacyActionResult> RequestDeletionAsync(CancellationToken ct = default)
        => RequestActionAsync("/api/privacy/telemetry/delete", ct);

    private async Task<TelemetryPrivacyActionResult> RequestActionAsync(string path, CancellationToken ct)
    {
        var response = await httpClient.PostAsync(path, content: null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ActionDto>(JsonOptions, ct).ConfigureAwait(false);
        return new TelemetryPrivacyActionResult(
            Accepted: dto?.Status is "accepted" or "pending",
            Message: dto?.Message ?? "accepted",
            RequestId: dto?.RequestId);
    }

    private static TelemetryPrivacyState MapState(ConsentDto? dto)
    {
        if (dto is null)
        {
            return new TelemetryPrivacyState(
                TelemetryConsentLevel.None,
                "Não coletar",
                "A coleta de telemetria está desabilitada.",
                null,
                null);
        }

        var level = Enum.TryParse<TelemetryConsentLevel>(dto.ConsentLevel, true, out var parsed)
            ? parsed
            : TelemetryConsentLevel.None;

        return new TelemetryPrivacyState(
            level,
            level switch
            {
                TelemetryConsentLevel.None => "Não coletar",
                TelemetryConsentLevel.Anonymous => "Coleta anônima",
                TelemetryConsentLevel.Full => "Coleta completa",
                _ => "Telemetria"
            },
            level switch
            {
                TelemetryConsentLevel.None => "Nenhum dado de telemetria será enviado.",
                TelemetryConsentLevel.Anonymous => "Apenas dados anonimizados serão enviados.",
                TelemetryConsentLevel.Full => "Dados completos com hash de usuário serão enviados.",
                _ => "Telemetria"
            },
            dto.GrantedAt,
            dto.RevokedAt);
    }

    private sealed record ConsentDto(string ConsentLevel, DateTimeOffset? GrantedAt, DateTimeOffset? RevokedAt);
    private sealed record ActionDto(string? RequestId, string Status, string Message);
}
