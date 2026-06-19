using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KrnlAI.Sidecar;

public sealed class KernelApiProxy(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<SidecarOptions> options,
    ILogger<KernelApiProxy> logger)
{
    private static readonly Meter Meter = new("KrnlAI.Sidecar.Proxy");
    private static readonly Counter<int> RequestsTotal = Meter.CreateCounter<int>("sidecar_proxy_requests_total",
        description: "Total proxy requests", unit: "{count}");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>("sidecar_proxy_duration_seconds",
        description: "Proxy request duration");

    private static readonly Random Jitter = new();
    private int _failureCount;
    private DateTime _circuitOpenUntil = DateTime.MinValue;
    private readonly object _circuitLock = new();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.KernelApi.BaseUrl);

    public async Task<T?> ProxyGetAsync<T>(string path, CancellationToken ct) where T : class
    {
        if (!IsConfigured) return null;
        return await ExecuteWithResilienceAsync<T>(HttpMethod.Get, path, null, ct);
    }

    public async Task<TResponse?> ProxyPostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
        where TResponse : class
    {
        if (!IsConfigured) return null;
        return await ExecuteWithResilienceAsync<TResponse>(HttpMethod.Post, path, body, ct);
    }

    public async Task<bool> PingAsync(CancellationToken ct)
    {
        if (!IsConfigured) return true;
        try
        {
            var client = httpClientFactory.CreateClient("kernel");
            var res = await client.GetAsync($"/health", ct);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<T?> ExecuteWithResilienceAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
        where T : class
    {
        var cacheKey = $"{method}:{path}";
        var sw = Stopwatch.StartNew();

        // Check circuit breaker
        if (IsCircuitOpen())
        {
            logger.LogWarning("Circuit breaker open for {Method} {Path}, serving from cache/stub", method, path);
            var cached = await GetFromCacheAsync<T>(cacheKey);
            if (cached is not null)
            {
                sw.Stop();
                RecordResult("fallback_cache", sw.Elapsed.TotalSeconds);
                return cached;
            }
            sw.Stop();
            RecordResult("fallback_stub", sw.Elapsed.TotalSeconds);
            return null;
        }

        var opts = options.Value.KernelApi;
        var baseUrl = opts.BaseUrl.TrimEnd('/');

        for (var attempt = 1; attempt <= Math.Max(1, opts.RetryCount); attempt++)
        {
            try
            {
                var client = httpClientFactory.CreateClient("kernel");
                HttpResponseMessage res;

                if (method == HttpMethod.Get)
                {
                    res = await client.GetAsync($"{baseUrl}{path}", ct);
                }
                else
                {
                    res = await client.PostAsJsonAsync($"{baseUrl}{path}", body, ct);
                }

                if (!res.IsSuccessStatusCode)
                {
                    logger.LogWarning("Proxy {Method} {Path} returned {Status} (attempt {Attempt}/{Max})",
                        method, path, (int)res.StatusCode, attempt, opts.RetryCount);

                    if (attempt < opts.RetryCount)
                    {
                        await BackoffDelay(attempt, ct);
                        continue;
                    }

                    RecordFailure();
                    var cached = await GetFromCacheAsync<T>(cacheKey);
                    if (cached is not null)
                    {
                        sw.Stop();
                        RecordResult("fallback_cache", sw.Elapsed.TotalSeconds);
                        return cached;
                    }
                    sw.Stop();
                    RecordResult("fallback_stub", sw.Elapsed.TotalSeconds);
                    return null;
                }

                var result = await res.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                if (result is not null && method == HttpMethod.Get)
                {
                    cache.Set(cacheKey, result, TimeSpan.FromSeconds(opts.CacheTtlSeconds));
                }

                RecordSuccess();
                sw.Stop();
                RecordResult("success", sw.Elapsed.TotalSeconds);
                return result;
            }
            catch (Exception ex) when (attempt < opts.RetryCount)
            {
                logger.LogWarning(ex, "Proxy {Method} {Path} failed (attempt {Attempt}/{Max}): {Msg}",
                    method, path, attempt, opts.RetryCount, ex.Message);
                await BackoffDelay(attempt, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Proxy {Method} {Path} failed after {Attempt} attempts: {Msg}",
                    method, path, opts.RetryCount, ex.Message);
                RecordFailure();
                var cached = await GetFromCacheAsync<T>(cacheKey);
                if (cached is not null)
                {
                    sw.Stop();
                    RecordResult("fallback_cache", sw.Elapsed.TotalSeconds);
                    return cached;
                }
                sw.Stop();
                RecordResult("error", sw.Elapsed.TotalSeconds);
                return null;
            }
        }

        sw.Stop();
        RecordResult("error", sw.Elapsed.TotalSeconds);
        return null;
    }

    private Task BackoffDelay(int attempt, CancellationToken ct)
    {
        var delay = (int)(Math.Pow(2, attempt - 1) * 100);
        delay += Jitter.Next(0, 50);
        return Task.Delay(delay, ct);
    }

    private bool IsCircuitOpen()
    {
        lock (_circuitLock)
        {
            if (_circuitOpenUntil > DateTime.UtcNow)
                return true;
            if (_circuitOpenUntil != DateTime.MinValue && _circuitOpenUntil <= DateTime.UtcNow)
            {
                _circuitOpenUntil = DateTime.MinValue;
                logger.LogInformation("Circuit breaker closed, resuming proxy requests");
            }
            return false;
        }
    }

    private void RecordSuccess()
    {
        lock (_circuitLock)
        {
            _failureCount = 0;
        }
    }

    private void RecordFailure()
    {
        var opts = options.Value.KernelApi;
        lock (_circuitLock)
        {
            _failureCount++;
            if (_failureCount >= opts.CircuitMinThroughput)
            {
                _circuitOpenUntil = DateTime.UtcNow.AddSeconds(opts.CircuitBreakDurationSeconds);
                logger.LogWarning("Circuit breaker opened for {Duration}s after {Failures} failures",
                    opts.CircuitBreakDurationSeconds, _failureCount);
            }
        }
    }

    private Task<T?> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        if (cache.TryGetValue<T>(cacheKey, out var cached) && cached is not null)
        {
            logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
            return Task.FromResult<T?>(cached);
        }
        return Task.FromResult<T?>(null);
    }

    private static void RecordResult(string result, double durationSec)
    {
        RequestsTotal.Add(1,
            new KeyValuePair<string, object?>("target", "kernel-api"),
            new KeyValuePair<string, object?>("result", result));
        DurationHistogram.Record(durationSec,
            new KeyValuePair<string, object?>("target", "kernel-api"));
    }
}
