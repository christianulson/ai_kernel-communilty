using System.Net.Http.Json;
using System.Text.Json;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Services;

public sealed class PoliciesService : IPoliciesService, IDisposable
{
    private readonly HttpClient _http;
    private string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private IReadOnlyList<Policy>? _cached;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public PoliciesService(HttpClient? http = null)
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
        catch
        {
            _baseUrl = "http://localhost:65335";
        }
        return _baseUrl;
    }

    public async Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastFetch < CacheTtl)
            return _cached;

        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/policies", ct);
            if (!response.IsSuccessStatusCode)
                return _cached ?? Array.Empty<Policy>();

            var result = await response.Content
                .ReadFromJsonAsync<List<Policy>>(JsonOpts, ct);

            if (result is not null)
            {
                _cached = result;
                _lastFetch = DateTime.UtcNow;
            }

            return _cached ?? Array.Empty<Policy>();
        }
        catch
        {
            return _cached ?? Array.Empty<Policy>();
        }
    }

    public async Task<bool> TogglePolicyAsync(string policyId, bool active, CancellationToken ct)
    {
        try
        {
            var payload = new { active };
            var response = await _http.PutAsJsonAsync(
                $"{GetBaseUrl()}/policies/{policyId}", payload, JsonOpts, ct);
            if (response.IsSuccessStatusCode)
                _cached = null;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
