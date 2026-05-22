using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class KanbanService : IKanbanService, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public KanbanService(HttpClient? http = null) : this(GetDefaultBaseUrl(), http) { }

    public KanbanService(string baseUrl, HttpClient? http = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = http ?? new HttpClient();
    }

    private static string GetDefaultBaseUrl()
    {
        try
        {
            var settings = new SettingsService();
            settings.Load();
            return settings.Endpoint.TrimEnd('/');
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return "http://localhost:65335";
        }
    }

    public async Task<KanbanResponse?> GetKanbanAsync(
        int daysBack = 10,
        string? domain = null,
        double? minPriority = null,
        string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/goals/kanban?daysBack={daysBack}";
            if (domain is not null) url += $"&domain={Uri.EscapeDataString(domain)}";
            if (minPriority.HasValue) url += $"&minPriority={minPriority.Value}";
            if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<KanbanResponse>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public async Task<bool> MoveCardAsync(string cardId, string newStatus, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { status = newStatus }, JsonOpts);
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/api/goals/{Uri.EscapeDataString(cardId)}/status")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
            var response = await _http.SendAsync(request, ct);
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
}
