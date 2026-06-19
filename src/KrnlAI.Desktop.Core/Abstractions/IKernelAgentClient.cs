using KrnlAI.Contracts.Contracts;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Cliente focado em execução e health do KrnlAI.</summary>
public interface IKernelAgentClient
{
    Task<AgentRunTransportResponse> RunAgentAsync(AgentRunTransportRequest request, CancellationToken cancellationToken = default);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
