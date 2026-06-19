using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KrnlAI.Cli.Services;

public sealed record RegistryPlugin(
    string Id,
    string Name,
    string Description,
    string Author,
    [property: JsonPropertyName("type")] string Type,
    string Url,
    string Risk,
    bool Verified,
    string Version);

public sealed record RegistryIndex(
    int Version,
    IReadOnlyList<RegistryPlugin> Plugins);

public sealed class PluginDiscoveryService(HttpClient httpClient)
{
    public async Task<RegistryIndex?> FetchIndexAsync(CancellationToken ct = default)
    {
        var index = await httpClient.GetFromJsonAsync<RegistryIndex>("registry.json", ct);
        return index;
    }

    public async Task<IReadOnlyList<RegistryPlugin>> SearchAsync(string term, CancellationToken ct = default)
    {
        var index = await FetchIndexAsync(ct);
        if (index is null) return [];

        var termLower = term.ToLowerInvariant();
        return [.. index.Plugins
            .Where(p => p.Id.Contains(termLower, StringComparison.OrdinalIgnoreCase) ||
                        p.Name.Contains(termLower, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(termLower, StringComparison.OrdinalIgnoreCase))];
    }

    public async Task<RegistryPlugin?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var index = await FetchIndexAsync(ct);
        return index?.Plugins.FirstOrDefault(p =>
            p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
