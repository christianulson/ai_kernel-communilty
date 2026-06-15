using System.Net.Http;
using System.Text.Json;

namespace KrnlAI.Desktop.App.Services;

public sealed class UpdateChecker
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private const string CurrentVersion = "2.1.0";

    public async Task<string?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _http.GetStringAsync("https://api.github.com/repos/krnlai/krnl-ai/releases/latest");
            var doc = JsonDocument.Parse(response);
            var latest = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            doc.Dispose();
            if (string.Compare(latest.TrimStart('v'), CurrentVersion.TrimStart('v'), StringComparison.OrdinalIgnoreCase) > 0)
                return latest;
            return null;
        }
        catch { return null; }
    }

    public string GetCurrentVersion() => CurrentVersion;
}
