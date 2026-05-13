namespace AIKernel.Sidecar;

/// <summary>
/// Solicitação de execução de um agente no modo standalone.
/// </summary>
/// <param name="Prompt">Texto de entrada do usuário</param>
/// <param name="Mode">Modo de operação (default: "standalone")</param>
public record AgentRunRequest(string? Prompt, string? Mode = "standalone");

/// <summary>
/// Resposta da execução de um agente no modo standalone.
/// </summary>
public class AgentRunResponse
{
    /// <summary>Texto narrativo gerado pelo agente</summary>
    public string? Narration { get; init; }
    /// <summary>Comando estruturado opcional</summary>
    public Dictionary<string, object>? Command { get; init; }
    /// <summary>Etapas de transporte para trace/debug</summary>
    public TransportStepDto[]? TransportSteps { get; init; }
    /// <summary>Estágios ativos durante a execução</summary>
    public string[]? ActiveStages { get; init; }
    /// <summary>Mensagem de erro, se houver</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Etapa de transporte usada para trace e depuração.
/// </summary>
public record TransportStepDto
{
    /// <summary>Rótulo da etapa</summary>
    public string Label { get; init; } = "";
    /// <summary>Detalhamento do resultado</summary>
    public string Detail { get; init; } = "";
    /// <summary>Indicador de sucesso</summary>
    public bool Ok { get; init; }
}
