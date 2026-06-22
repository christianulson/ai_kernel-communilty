using System.Text.Json;
using KrnlAI.Sdk;
using KrnlAI.Sdk.Models;

namespace KrnlAI.VisualStudio.Services;

public sealed class KernelClientService(
    HttpClient? http = null,
    IVsOperationTracker? debugTracker = null) : IKernelClientService, IDisposable
{
    private readonly IVsOperationTracker _debugTracker = debugTracker ?? new VsOperationTracker();
    private KrnlAIClient? _client;
    private readonly HttpClient _http = http ?? new HttpClient();
    private ConnectionState _state = ConnectionState.Disconnected;
    private string? _baseUrl;
    private const int MaxRetries = 3;

    public string? BaseUrl => _baseUrl;

    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(value);
        }
    }

    public event Action<ConnectionState>? StateChanged;

    public async Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("kernel.connect", endpoint);

        State = ConnectionState.Connecting;

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                _baseUrl = endpoint.TrimEnd('/');
                _client = new KrnlAIClient(_baseUrl, _http);

                var health = await _client.HealthCheckAsync(ct);
                if (health.Ok)
                {
                    State = ConnectionState.Connected;
                    op.SetResult("Connected");
                    return true;
                }
            }
            catch (Exception) when (attempt < MaxRetries - 1)
            {
                await Task.Delay(1000 * (attempt + 1), ct);
            }
            catch (Exception ex)
            {
                op.SetError(ex.Message);
                // Fall through — will return false after the loop
            }
        }

        State = ConnectionState.Failed;
        op.SetResult("Failed after retries");
        return false;
    }

    public Task DisconnectAsync()
    {
        _client = null;
        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public async Task<AgentRunResponse> RunAgentAsync(string goal, AgentRunRequest? request = null, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("kernel.run_agent", goal);

        if (_client is null)
        {
            op.SetError("Not connected");
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
        }

        var req = request ?? new AgentRunRequest(
            Goal: goal,
            MaxSteps: 10,
            ApproveHighRisk: false,
            ApproveMetaCriticStops: false
        );

        try
        {
            var response = await _client.AgentRunAsync(req, ct);
            op.SetResult(response.Status ?? "completed");
            return response;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public async Task<MemorySearchResponse> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("kernel.search_memory", query);

        if (_client is null)
        {
            op.SetError("Not connected");
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
        }

        try
        {
            var response = await _client.MemorySearchAsync(new MemorySearchRequest(Query: query, TopK: topK), ct);
            op.SetResult($"{response.Hits?.Count ?? 0} hits");
            return response;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public async Task<HealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        using var op = _debugTracker.Start("kernel.health_check");

        if (_client is null)
        {
            op.SetError("Not connected");
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
        }

        try
        {
            var result = await _client.HealthCheckAsync(ct);
            op.SetResult(result.Ok ? "Healthy" : "Unhealthy");
            return result;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            throw;
        }
    }

    public async Task<string?> GetEmotionalMoodAsync(CancellationToken ct = default)
    {
        if (_baseUrl is null) return null;

        try
        {
            var userId = Environment.GetEnvironmentVariable("KRNL_USER_ID") ?? "dev-user";
            var url = $"{_baseUrl}/profile/emotional?userId={userId}";
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var valence = root.GetProperty("valence").GetDouble();
            var arousal = root.GetProperty("arousal").GetDouble();

            if (valence > 0.3)
                return arousal < 0.4 ? "😌 Tranquilo" : "⚡ Animado";
            if (valence < -0.3)
                return arousal < 0.4 ? "😮‍💨 Cansado" : "😰 Tenso";
            return arousal >= 0.4 ? "🧐 Atento" : "😐 Neutro";
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
