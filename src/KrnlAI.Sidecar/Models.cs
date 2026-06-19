using KrnlAI.Contracts.Contracts;

namespace KrnlAI.Sidecar;

/// <summary>
/// Solicitação de execução de um agente no modo standalone.
/// </summary>
/// <param name="Prompt">Texto de entrada do usuário</param>
/// <param name="Mode">Modo de operação (default: "standalone")</param>
/// <param name="Goal">Compatibilidade com SDK .NET, que envia goal em vez de prompt.</param>
public record AgentRunRequest(string? Prompt, string? Mode = "standalone", string? Goal = null);
