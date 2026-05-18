using System.Text;
using System.Text.Json;
using KrnlAi.Sdk.Models;

namespace KrnlAi.Sdk;

public class KrnlAiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public KrnlAiClient(string baseUrl, HttpClient? http = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = http ?? new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<T> RequestAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}{path}";
        using var msg = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(msg, ct);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw (int)response.StatusCode switch
            {
                401 => new KrnlAiAuthenticationException(),
                429 => new KrnlAiRateLimitException(),
                >= 400 and < 500 => new KrnlAiValidationException($"Request failed: {responseBody}", (int)response.StatusCode),
                >= 500 => new KrnlAiServerException($"Server error: {responseBody}", (int)response.StatusCode),
                _ => new KrnlAiException($"Request failed: {responseBody}", (int)response.StatusCode)
            };
        }

        return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions)
            ?? throw new KrnlAiException("Empty response");
    }

    public Task<AgentRunResponse> AgentRunAsync(AgentRunRequest req, CancellationToken ct = default)
        => RequestAsync<AgentRunResponse>(HttpMethod.Post, "/agent/run", req, ct);

    public Task<AgentRunStatus> AgentGetStatusAsync(string runId, CancellationToken ct = default)
        => RequestAsync<AgentRunStatus>(HttpMethod.Get, $"/agent/status/{runId}", null, ct);

    public Task<MemorySearchResponse> MemorySearchAsync(MemorySearchRequest req, CancellationToken ct = default)
        => RequestAsync<MemorySearchResponse>(HttpMethod.Post, "/memory/search", req, ct);

    public Task<MemoryIngestResponse> MemoryIngestAsync(MemoryIngestRequest req, CancellationToken ct = default)
        => RequestAsync<MemoryIngestResponse>(HttpMethod.Post, "/memory/upsert", req, ct);

    public Task<PersistentGoal[]> GoalsListAsync(CancellationToken ct = default)
        => RequestAsync<PersistentGoal[]>(HttpMethod.Get, "/goals/active", null, ct);

    public Task<PersistentGoal> GoalsCreateAsync(CreateGoalRequest req, CancellationToken ct = default)
        => RequestAsync<PersistentGoal>(HttpMethod.Post, "/goals", req, ct);

    public Task GoalsUpdateStatusAsync(string goalId, string status, CancellationToken ct = default)
        => RequestAsync<object>(HttpMethod.Post, $"/goals/{goalId}/{status}", null, ct);

    public Task<EpisodeListItem[]> EpisodesListAsync(string? userId = null, CancellationToken ct = default)
    {
        var query = userId is not null ? $"?userId={Uri.EscapeDataString(userId)}" : "";
        return RequestAsync<EpisodeListItem[]>(HttpMethod.Get, $"/episodes{query}", null, ct);
    }

    public Task<EpisodeDetail> EpisodesGetAsync(string episodeId, CancellationToken ct = default)
        => RequestAsync<EpisodeDetail>(HttpMethod.Get, $"/episodes/{episodeId}", null, ct);

    public Task<HealthStatus> HealthCheckAsync(CancellationToken ct = default)
        => RequestAsync<HealthStatus>(HttpMethod.Get, "/health", null, ct);

    public Task<MetricsSummary> MetricsSummaryAsync(CancellationToken ct = default)
        => RequestAsync<MetricsSummary>(HttpMethod.Get, "/agent/metrics/summary", null, ct);

    public void Dispose()
    {
        _http.Dispose();
    }
}
