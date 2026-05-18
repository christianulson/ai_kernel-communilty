using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Cliente focado em execução e health do Kernel.</summary>
public interface IKernelAgentClient
{
    Task<AgentRunResponse> RunAgentAsync(AgentRunRequest request, CancellationToken cancellationToken = default);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
