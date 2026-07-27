using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.Sidecar.Services;

public sealed class SearchResult
{
    public string Title { get; init; } = "";
    public string Snippet { get; init; } = "";
    public string Url { get; init; } = "";
    public string Source { get; init; } = "";
}

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct);
}

public sealed class SearchService : ISearchService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IHttpClientFactory http, ILogger<SearchService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct)
    {
        var results = new List<SearchResult>();

        try
        {
            var duck = await SearchDuckDuckGoAsync(query, ct).ConfigureAwait(false);
            results.AddRange(duck);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DuckDuckGo search failed for {Query}", query);
        }

        if (results.Count == 0)
        {
            try
            {
                var wiki = await SearchWikipediaAsync(query, ct).ConfigureAwait(false);
                results.AddRange(wiki);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wikipedia search failed for {Query}", query);
            }
        }

        _logger.LogInformation("Search for '{Query}' returned {Count} results", query, results.Count);
        return results.AsReadOnly();
    }

    private async Task<IReadOnlyList<SearchResult>> SearchDuckDuckGoAsync(string query, CancellationToken ct)
    {
        var client = _http.CreateClient("search");
        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
        var json = await client.GetFromJsonAsync<JsonElement>(url, ct).ConfigureAwait(false);
        var results = new List<SearchResult>();

        if (json.TryGetProperty("AbstractText", out var abs) && abs.ValueKind == JsonValueKind.String)
        {
            var absText = abs.GetString() ?? "";
            if (!string.IsNullOrWhiteSpace(absText))
            {
                results.Add(new SearchResult
                {
                    Title = json.TryGetProperty("Heading", out var h) ? h.GetString() ?? "" : "",
                    Snippet = absText,
                    Url = json.TryGetProperty("AbstractURL", out var u) ? u.GetString() ?? "" : "",
                    Source = "DuckDuckGo"
                });
            }
        }

        if (json.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
        {
            foreach (var topic in topics.EnumerateArray())
            {
                if (topic.ValueKind != JsonValueKind.Object) continue;
                if (topic.TryGetProperty("Text", out var txt) && txt.ValueKind == JsonValueKind.String)
                {
                    var title = topic.TryGetProperty("FirstURL", out var fu) ? fu.GetString() ?? "" : "";
                    results.Add(new SearchResult
                    {
                        Title = title,
                        Snippet = txt.GetString() ?? "",
                        Url = title,
                        Source = "DuckDuckGo"
                    });
                }
                if (results.Count >= 5) break;
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<SearchResult>> SearchWikipediaAsync(string query, CancellationToken ct)
    {
        var client = _http.CreateClient("search");
        var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(query)}";
        var json = await client.GetFromJsonAsync<JsonElement>(url, ct).ConfigureAwait(false);
        var results = new List<SearchResult>();

        if (json.TryGetProperty("extract", out var ext) && ext.ValueKind == JsonValueKind.String)
        {
            results.Add(new SearchResult
            {
                Title = json.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Snippet = ext.GetString() ?? "",
                Url = json.TryGetProperty("content_urls", out var cu) && cu.TryGetProperty("desktop", out var d) && d.TryGetProperty("page", out var p) ? p.GetString() ?? "" : "",
                Source = "Wikipedia"
            });
        }

        return results;
    }
}
