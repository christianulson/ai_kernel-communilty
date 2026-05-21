using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class EpisodesService : IEpisodesService, IDisposable
{
    private readonly HttpClient _http;
    private string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private IReadOnlyList<Episode>? _cached;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public EpisodesService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    private string GetBaseUrl()
    {
        if (_baseUrl is not null) return _baseUrl;
        try
        {
            var settings = new SettingsService();
            settings.Load();
            _baseUrl = settings.Endpoint.TrimEnd('/');
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            _baseUrl = "http://localhost:65335";
        }
        return _baseUrl;
    }

    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastFetch < CacheTtl)
            return _cached;

        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/episodes", ct);
            if (!response.IsSuccessStatusCode)
                return _cached ?? Array.Empty<Episode>();

            var result = await response.Content
                .ReadFromJsonAsync<List<Episode>>(JsonOpts, ct);

            if (result is not null)
            {
                _cached = result;
                _lastFetch = DateTime.UtcNow;
            }

            return _cached ?? Array.Empty<Episode>();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return _cached ?? Array.Empty<Episode>();
        }
    }

    public async Task<Episode?> GetEpisodeDetailsAsync(string episodeId, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(
                $"{GetBaseUrl()}/episodes/{episodeId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content
                .ReadFromJsonAsync<Episode>(JsonOpts, ct);
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
}
