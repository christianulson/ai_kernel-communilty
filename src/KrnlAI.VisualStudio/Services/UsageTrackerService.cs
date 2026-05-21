using System.IO;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class UsageTrackerService : IUsageTrackerService
{
    private UsageStats _stats = new(0, 0, 0, 0, 0, TimeSpan.Zero, 0);
    private readonly DateTime _sessionStart = DateTime.UtcNow;
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UsageStats Stats => _stats;
    public event Action<UsageStats>? StatsChanged;

    public UsageTrackerService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KrnlAI", "usage.json");
        LoadFromDisk();
    }

    public void TrackCommand(string command)
    {
        _stats = _stats with
        {
            CommandInvocations = _stats.CommandInvocations + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    public void TrackAgentRun(int tokensIn, int tokensOut)
    {
        _stats = _stats with
        {
            AgentRuns = _stats.AgentRuns + 1,
            TokensIn = _stats.TokensIn + tokensIn,
            TokensOut = _stats.TokensOut + tokensOut,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    public void TrackError(string error)
    {
        _stats = _stats with
        {
            Errors = _stats.Errors + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    public void TrackApiCall()
    {
        _stats = _stats with
        {
            ApiCalls = _stats.ApiCalls + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_stats, JsonOpts);
            await Task.Run(() => File.WriteAllText(_filePath, json), ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KrnlAI] Usage save failed: {ex.Message}");
        }
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        _stats = new UsageStats(0, 0, 0, 0, 0, TimeSpan.Zero, 0);
        StatsChanged?.Invoke(_stats);

        try
        {
            if (File.Exists(_filePath))
            {
                await Task.Run(() => File.Delete(_filePath), ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KrnlAI] Usage reset failed: {ex.Message}");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<UsageStats>(json, JsonOpts);
            if (loaded is not null)
                _stats = loaded with { SessionDuration = DateTime.UtcNow - _sessionStart };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KrnlAI] Usage load failed: {ex.Message}");
        }
    }
}
