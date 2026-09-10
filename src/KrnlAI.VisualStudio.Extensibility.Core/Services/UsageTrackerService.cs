using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>Usage statistics snapshot.</summary>
public sealed record UsageStats(
    int CommandInvocations,
    int AgentRuns,
    int TokensIn,
    int TokensOut,
    int Errors,
    TimeSpan SessionDuration,
    int ApiCalls);

/// <summary>
/// Usage tracking (portable; persistence to a JSON file when a path is provided).
/// </summary>
public sealed class UsageTrackerService
{
    private UsageStats _stats = new(0, 0, 0, 0, 0, TimeSpan.Zero, 0);
    private readonly DateTime _sessionStart = DateTime.UtcNow;
    private readonly string? _filePath;
    private readonly ILogger<UsageTrackerService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Creates a new instance. Pass <paramref name="filePath"/> to enable persistence.</summary>
    public UsageTrackerService(string? filePath, ILogger<UsageTrackerService>? logger = null)
    {
        _filePath = filePath;
        _logger = logger ?? NullLogger<UsageTrackerService>.Instance;
        if (_filePath is not null)
            LoadFromDisk();
    }

    /// <summary>Current stats.</summary>
    public UsageStats Stats => _stats;

    /// <summary>Raised when stats change.</summary>
    public event Action<UsageStats>? StatsChanged;

    /// <summary>Tracks a command invocation.</summary>
    public void TrackCommand(string command)
    {
        _stats = _stats with
        {
            CommandInvocations = _stats.CommandInvocations + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    /// <summary>Tracks an agent run with token usage.</summary>
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

    /// <summary>Tracks an error.</summary>
    public void TrackError(string error)
    {
        _stats = _stats with
        {
            Errors = _stats.Errors + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    /// <summary>Tracks an API call.</summary>
    public void TrackApiCall()
    {
        _stats = _stats with
        {
            ApiCalls = _stats.ApiCalls + 1,
            SessionDuration = DateTime.UtcNow - _sessionStart
        };
        StatsChanged?.Invoke(_stats);
    }

    /// <summary>Persists stats to disk (no-op when no file path was provided).</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (_filePath is null)
            return;

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
            _logger.LogError(ex, "Failed to save usage stats");
        }
    }

    /// <summary>Resets stats and removes the persisted file.</summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        _stats = new UsageStats(0, 0, 0, 0, 0, TimeSpan.Zero, 0);
        StatsChanged?.Invoke(_stats);

        if (_filePath is null)
            return;

        try
        {
            if (File.Exists(_filePath))
                await Task.Run(() => File.Delete(_filePath), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete usage stats file");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (_filePath is null || !File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<UsageStats>(json, JsonOpts);
            if (loaded is not null)
                _stats = loaded with { SessionDuration = DateTime.UtcNow - _sessionStart };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load usage stats");
        }
    }
}