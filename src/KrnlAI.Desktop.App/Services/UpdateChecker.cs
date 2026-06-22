using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Services;

public sealed class UpdateChecker
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private const string CurrentVersion = "2.1.0";
    private bool _isDownloading;

    public async Task<string?> CheckForUpdatesAsync()
    {
        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("KrnlAI-Desktop/2.1.0");
            var response = await _http.GetStringAsync("https://api.github.com/repos/krnlai/krnl-ai/releases/latest");
            var doc = JsonDocument.Parse(response);
            var latest = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var assets = doc.RootElement.TryGetProperty("assets", out var a) ? a : default;
            doc.Dispose();
            if (string.Compare(latest.TrimStart('v'), CurrentVersion.TrimStart('v'), StringComparison.OrdinalIgnoreCase) > 0)
                return latest;
            return null;
        }
        catch { return null; }
    }

    public async Task DownloadAndInstallAsync(string version)
    {
        if (_isDownloading) return;
        _isDownloading = true;
        try
        {
            var platform = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "windows",
                PlatformID.MacOSX => "macos",
                _ => "linux"
            };
            var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var assetName = $"KrnlAI-Desktop-{version}-{platform}-{arch}";
            if (platform == "windows") assetName += ".exe";
            else if (platform == "macos") assetName += ".dmg";
            else assetName += ".AppImage";

            // Try to get download URL from GitHub release API
            var downloadUrl = $"https://github.com/krnlai/krnl-ai/releases/download/{version}/{assetName}";

            // Download to temp directory
            var tempDir = System.IO.Path.GetTempPath();
            var installerPath = System.IO.Path.Combine(tempDir, assetName);

            using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                // Fallback: open releases page
                Process.Start(new ProcessStartInfo("https://github.com/krnlai/krnl-ai/releases/latest") { UseShellExecute = true });
                return;
            }

            await using var fs = new System.IO.FileStream(installerPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
            await response.Content.CopyToAsync(fs);
            await fs.FlushAsync();
            fs.Close();

            // Launch installer
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Update download failed: {ex.Message}");
            // Fallback: open releases page
            try { Process.Start(new ProcessStartInfo("https://github.com/krnlai/krnl-ai/releases/latest") { UseShellExecute = true }); }
            catch { }
        }
        finally
        {
            _isDownloading = false;
        }
    }

    public string GetCurrentVersion() => CurrentVersion;
}
