using System.Diagnostics;

namespace KrnlAI.VisualStudio.Services;

public sealed class CloudDelegationService : ICloudDelegationService
{
    private readonly ISettingsService _settings;
    private readonly Queue<long> _recentLatencies = new();
    private const int MaxLatencySamples = 10;
    private const double HighLatencyThresholdMs = 5000;
    private int _consecutiveErrors;
    private const int MaxConsecutiveErrors = 3;

    public CloudMode Mode => _settings.CloudMode;
    public bool IsUsingCloud { get; private set; }

    public CloudDelegationService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> localAction,
        Func<Task<T>> cloudAction,
        CancellationToken ct = default)
    {
        var mode = _settings.CloudMode;

        if (mode == CloudMode.AlwaysLocal)
            return await ExecuteMeasuredAsync(localAction, false);

        if (mode == CloudMode.AlwaysCloud)
        {
            IsUsingCloud = true;
            return await cloudAction();
        }

        if (ShouldOffloadToCloud())
        {
            IsUsingCloud = true;
            try
            {
                return await cloudAction();
            }
            catch
            {
                IsUsingCloud = false;
                return await ExecuteMeasuredAsync(localAction, false);
            }
        }

        return await ExecuteMeasuredAsync(localAction, false);
    }

    private async Task<T> ExecuteMeasuredAsync<T>(Func<Task<T>> action, bool isRetry)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            RecordLatency(sw.ElapsedMilliseconds);
            _consecutiveErrors = 0;
            return result;
        }
        catch
        {
            _consecutiveErrors++;
            sw.Stop();
            RecordLatency(sw.ElapsedMilliseconds);
            throw;
        }
    }

    private void RecordLatency(long ms)
    {
        _recentLatencies.Enqueue(ms);
        if (_recentLatencies.Count > MaxLatencySamples)
            _recentLatencies.Dequeue();
    }

    private bool ShouldOffloadToCloud()
    {
        if (_consecutiveErrors >= MaxConsecutiveErrors)
            return true;

        if (_recentLatencies.Count >= 3)
        {
            var avg = _recentLatencies.Average();
            if (avg > HighLatencyThresholdMs)
                return true;
        }

        return false;
    }
}
