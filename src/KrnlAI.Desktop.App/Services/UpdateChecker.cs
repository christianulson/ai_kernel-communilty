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
    private long _lastDownloadProgress;

    public event Action<string, int>? ProgressChanged;  // status, percent
    public event Action<string, string>? DownloadCompleted; // version, installerPath
    public event Action<string>? ErrorOccurred;

    public async Task<string?> CheckForUpdatesAsync()
    {
        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("KrnlAI-Desktop/2.1.0");
            var response = await _http.GetStringAsync("https://api.github.com/repos/krnlai/krnl-ai/releases/latest").ConfigureAwait(false);
            var doc = JsonDocument.Parse(response);
            var latest = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var releaseUrl = doc.RootElement.TryGetProperty("html_url", out var url) ? url.GetString() : null;
            doc.Dispose();

            if (string.Compare(latest.TrimStart('v'), CurrentVersion.TrimStart('v'), StringComparison.OrdinalIgnoreCase) > 0)
                return latest;
            return null;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Update check failed: {ex.Message}");
            return null;
        }
    }

    public async Task DownloadAndInstallAsync(string version)
    {
        if (_isDownloading) return;
        _isDownloading = true;
        _lastDownloadProgress = 0;

        try
        {
            ProgressChanged?.Invoke("Preparing download...", 0);

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

            var downloadUrl = $"https://github.com/krnlai/krnl-ai/releases/download/{version}/{assetName}";
            var tempDir = System.IO.Path.GetTempPath();
            var installerPath = System.IO.Path.Combine(tempDir, assetName);

            ProgressChanged?.Invoke("Downloading update...", 10);

            using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Process.Start(new ProcessStartInfo("https://github.com/krnlai/krnl-ai/releases/latest") { UseShellExecute = true });
                ErrorOccurred?.Invoke("Download URL not found. Opening browser.");
                return;
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            await using var fs = new System.IO.FileStream(installerPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
            await using var stream = (await response.Content.ReadAsStreamAsync().ConfigureAwait(false));

            var buffer = new byte[81920];
            long bytesRead = 0;
            int bytes;

            while ((bytes = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytes).ConfigureAwait(false);
                bytesRead += bytes;

                if (totalBytes > 0)
                {
                    var percent = (int)(bytesRead * 100 / totalBytes);
                    if (percent > _lastDownloadProgress + 5 || percent >= 100)
                    {
                        _lastDownloadProgress = percent;
                        ProgressChanged?.Invoke($"Downloading... {percent}%", percent);
                    }
                }
            }

            await fs.FlushAsync().ConfigureAwait(false);
            fs.Close();

            ProgressChanged?.Invoke("Download complete. Launching installer...", 100);
            DownloadCompleted?.Invoke(version, installerPath);

            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Update download failed: {ex.Message}");
            ErrorOccurred?.Invoke($"Download failed: {ex.Message}");
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
