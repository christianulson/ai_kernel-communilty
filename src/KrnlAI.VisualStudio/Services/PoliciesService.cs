using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class PoliciesService(HttpClient? http = null) : IPoliciesService, IDisposable
{
    private readonly HttpClient _http = http ?? new HttpClient();
    private string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private IReadOnlyList<Policy>? _cached;
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

    public async Task<IReadOnlyList<Policy>> GetPoliciesAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastFetch < CacheTtl)
            return _cached;

        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/policy/list", ct);
            if (!response.IsSuccessStatusCode)
                return _cached ?? [];

            var result = await response.Content
                .ReadFromJsonAsync<PolicyListDto>(JsonOpts, ct);

            if (result is not null)
            {
                _cached = [.. result.Policies.Select(p => new Policy(
                    p.Id,
                    p.Name,
                    p.Description ?? p.Content ?? "",
                    p.Domain,
                    p.IsActive,
                    p.Score ?? 0))];
                _lastFetch = DateTime.UtcNow;
            }

            return _cached ?? [];
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return _cached ?? [];
        }
    }

    public async Task<bool> TogglePolicyAsync(string policyId, bool active, CancellationToken ct)
    {
        try
        {
            var payload = new { active };
            var response = await _http.PutAsJsonAsync(
                $"{GetBaseUrl()}/policy/{policyId}", payload, JsonOpts, ct);
            if (response.IsSuccessStatusCode)
                _cached = null;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return false;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private sealed record PolicyListDto(List<PolicyDto> Policies, int TotalCount);
    private sealed record PolicyDto(
        string Id,
        string Name,
        string Domain,
        bool IsActive,
        string? Description,
        string? Content,
        double? Score);
}
