using KrnlAI.Sdk;
using KrnlAI.Sdk.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>Kernel connection state.</summary>
public enum ConnectionState
{
    /// <summary>Not connected.</summary>
    Disconnected,

    /// <summary>Connection attempt in progress.</summary>
    Connecting,

    /// <summary>Connected and healthy.</summary>
    Connected,

    /// <summary>Connection attempt failed.</summary>
    Failed
}

/// <summary>Client for the Krnl-AI kernel API.</summary>
public interface IKernelClientService
{
    /// <summary>Current connection state.</summary>
    ConnectionState State { get; }

    /// <summary>Raised when the connection state changes.</summary>
    event Action<ConnectionState>? StateChanged;

    /// <summary>Connects to the kernel endpoint, verifying health.</summary>
    Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default);

    /// <summary>Runs an agent for the given goal.</summary>
    Task<AgentRunResponse> RunAgentAsync(string goal, AgentRunRequest? request = null, CancellationToken ct = default);

    /// <summary>Base URL of the connected kernel, if any.</summary>
    string? BaseUrl { get; }
}

/// <summary>
/// Kernel client implementation (portable, no VS SDK; uses KrnlAI.Sdk).
/// </summary>
public sealed class KernelClientService : IKernelClientService, IDisposable
{
    private readonly ILogger<KernelClientService> _logger;
    private readonly HttpClient _http;
    private readonly int _maxRetries;
    private KrnlAIClient? _client;
    private ConnectionState _state = ConnectionState.Disconnected;
    private string? _baseUrl;

    /// <summary>Creates a new instance.</summary>
    public KernelClientService(
        HttpClient? http = null,
        int maxRetries = 3,
        ILogger<KernelClientService>? logger = null)
    {
        _http = http ?? new HttpClient();
        _maxRetries = Math.Max(1, maxRetries);
        _logger = logger ?? NullLogger<KernelClientService>.Instance;
    }

    /// <inheritdoc/>
    public string? BaseUrl => _baseUrl;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public event Action<ConnectionState>? StateChanged;

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
    {
        State = ConnectionState.Connecting;

        for (var attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                _baseUrl = endpoint.TrimEnd('/');
                _client = new KrnlAIClient(_baseUrl, _http);

                var health = await _client.HealthCheckAsync(ct);
                if (health.Ok)
                {
                    State = ConnectionState.Connected;
                    _logger.LogInformation("Connected to kernel at {BaseUrl}", _baseUrl);
                    return true;
                }
            }
            catch (Exception ex) when (attempt < _maxRetries - 1)
            {
                _logger.LogWarning(ex, "Kernel connect attempt {Attempt} failed; retrying", attempt + 1);
                await Task.Delay(1000 * (attempt + 1), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kernel connect failed at {BaseUrl}", _baseUrl);
            }
        }

        State = ConnectionState.Failed;
        return false;
    }

    /// <inheritdoc/>
    public async Task<AgentRunResponse> RunAgentAsync(
        string goal,
        AgentRunRequest? request = null,
        CancellationToken ct = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        var req = request ?? new AgentRunRequest(
            Goal: goal,
            MaxSteps: 10,
            ApproveHighRisk: false,
            ApproveMetaCriticStops: false);

        var response = await _client.AgentRunAsync(req, ct);
        _logger.LogInformation("Agent run completed: {Status}", response.Status ?? "completed");
        return response;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _http.Dispose();
    }
}