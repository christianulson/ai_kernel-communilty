using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class BacklogService(string baseUrl, HttpClient? http = null) : IBacklogService, IDisposable
{
    private readonly HttpClient _http = http ?? new HttpClient();
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BacklogService(HttpClient? http = null) : this(GetDefaultBaseUrl(), http) { }

    private static string GetDefaultBaseUrl()
    {
        try
        {
            var settings = new SettingsService();
            settings.Load();
            return KernelEndpointResolver.Resolve(settings.RuntimeMode, settings.Endpoint, settings.SidecarPort);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return "http://localhost:5235";
        }
    }

    public async Task<IReadOnlyList<BacklogItem>?> GetItemsAsync(
        string? status = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/backlog";
            if (status is not null)
                url += $"?status={Uri.EscapeDataString(status)}";

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<IReadOnlyList<BacklogItem>>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public async Task<BacklogItem?> CreateItemAsync(
        string title,
        string? description,
        string priority,
        IReadOnlyList<string>? tags,
        CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                title,
                description,
                priority,
                tags
            };
            var json = JsonSerializer.Serialize(body, JsonOpts);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/backlog")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<BacklogItem>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public async Task<bool> MoveItemAsync(
        string id,
        string newStatus,
        CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { status = newStatus }, JsonOpts);
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/api/backlog/{Uri.EscapeDataString(id)}/status")
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

    public async Task<bool> DeleteItemAsync(
        string id,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/api/backlog/{Uri.EscapeDataString(id)}", ct);
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
