using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class DashboardService(HttpClient? http = null) : IDashboardService, IDisposable
{
    private readonly HttpClient _http = http ?? new HttpClient();
    private string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DashboardScorecard? _cachedScorecard;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private string GetBaseUrl()
    {
        if (_baseUrl is not null) return _baseUrl;
        try
        {
            var settings = new SettingsService();
            settings.Load();
            _baseUrl = KernelEndpointResolver.Resolve(settings.RuntimeMode, settings.Endpoint, settings.SidecarPort);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            _baseUrl = "http://localhost:5235";
        }
        return _baseUrl;
    }

    public async Task<DashboardScorecard?> GetScorecardAsync(CancellationToken ct)
    {
        if (_cachedScorecard is not null && DateTime.UtcNow - _lastFetch < CacheTtl)
            return _cachedScorecard;

        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/agent/metrics/scorecard", ct);
            if (!response.IsSuccessStatusCode)
                return _cachedScorecard;

            var dto = await response.Content
                .ReadFromJsonAsync<ScorecardDto>(JsonOpts, ct);
            var result = dto is null
                ? null
                : new DashboardScorecard(dto.Reliability, dto.Efficiency, dto.Safety, dto.AntiLoop, dto.Governance);

            if (result is not null)
            {
                _cachedScorecard = result;
                _lastFetch = DateTime.UtcNow;
            }

            return result;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return _cachedScorecard;
        }
    }

    public async Task<SystemHealth?> GetHealthAsync(CancellationToken ct)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _http.GetAsync($"{GetBaseUrl()}/health", ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
                return new SystemHealth("Unreachable", null, null, null);

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(), cancellationToken: ct);
            var root = doc.RootElement;

            var status = root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True
                ? "OK"
                : root.TryGetProperty("status", out var statusProp)
                    && (string.Equals(statusProp.GetString(), "ok", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(statusProp.GetString(), "healthy", StringComparison.OrdinalIgnoreCase))
                    ? "OK"
                    : "Warning";

            return new SystemHealth(
                status,
                root.TryGetProperty("ts", out var ts) ? ts.GetString() : null,
                null,
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return new SystemHealth("Unreachable", null, null, null);
        }
    }

    public async Task<string?> GetMoodAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/profile/emotional", ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(), cancellationToken: ct);
            var root = doc.RootElement;

            var valence = root.GetProperty("valence").GetDouble();
            var arousal = root.GetProperty("arousal").GetDouble();

            if (valence > 0.3)
                return arousal < 0.4 ? "😌 Calm" : "⚡ Excited";
            if (valence < -0.3)
                return arousal < 0.4 ? "😮‍💨 Tired" : "😰 Tense";
            return arousal >= 0.4 ? "🧐 Focused" : "😐 Neutral";
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private sealed record ScorecardDto(
        double Reliability,
        double Efficiency,
        double Safety,
        double AntiLoop,
        double Governance,
        double Overall);
}
