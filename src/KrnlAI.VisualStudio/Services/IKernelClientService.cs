using KrnlAI.Sdk.Models;

namespace KrnlAI.VisualStudio.Services;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Failed,
}

public interface IKernelClientService
{
    ConnectionState State { get; }
    event Action<ConnectionState>? StateChanged;

    Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default);
    Task DisconnectAsync();
    Task<AgentRunResponse> RunAgentAsync(string goal, AgentRunRequest? request = null, CancellationToken ct = default);
    Task<MemorySearchResponse> SearchMemoryAsync(string query, int topK = 10, CancellationToken ct = default);
    Task<HealthStatus> CheckHealthAsync(CancellationToken ct = default);
    Task<string?> GetEmotionalMoodAsync(CancellationToken ct = default);
    string? BaseUrl { get; }
}
