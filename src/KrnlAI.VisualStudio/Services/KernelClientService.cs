using System.Text.Json;
using KrnlAI.Sdk;
using KrnlAI.Sdk.Models;

namespace KrnlAI.VisualStudio.Services;

public sealed class KernelClientService : IKernelClientService, IDisposable
{
    private KrnlAIClient? _client;
    private readonly HttpClient _http;
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

    public KernelClientService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    public async Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        State = ConnectionState.Connecting;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                _baseUrl = endpoint.TrimEnd('/');
                _client = new KrnlAIClient(_baseUrl, _http);

                var health = await _client.HealthCheckAsync(ct);
                if (health.Ok)
                {
                    State = ConnectionState.Connected;
                    return true;
                }
            }
            catch
            {
                if (attempt < MaxRetries - 1)
                    await Task.Delay(1000 * (attempt + 1), ct);
            }
        }

        State = ConnectionState.Failed;
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
        if (_client is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        var req = request ?? new AgentRunRequest(
            Goal: goal,
            MaxSteps: 10,
            ApproveHighRisk: false,
            ApproveMetaCriticStops: false
        );

        return await _client.AgentRunAsync(req, ct);
    }

    public async Task<MemorySearchResponse> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        return await _client.MemorySearchAsync(new MemorySearchRequest(Query: query, TopK: topK), ct);
    }

    public async Task<HealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        return await _client.HealthCheckAsync(ct);
    }

    public async Task<string?> GetEmotionalMoodAsync(CancellationToken ct = default)
    {
        if (_baseUrl is null) return null;

        try
        {
            var url = $"{_baseUrl}/profile/emotional?userId=dev-user";
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
