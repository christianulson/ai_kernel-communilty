namespace Kernel.Contracts;

/// <summary>
/// Request para pergunta LLM com RAG.
/// </summary>
/// <param name="Text">Texto da pergunta</param>
public sealed record LlmAskRequest(string Text);

/// <summary>
/// Resposta de pergunta LLM com RAG.
/// </summary>
/// <param name="Reply">Resposta narrada em texto</param>
/// <param name="RawResult">Resultado bruto do Kernel</param>
public sealed record LlmAskResponse(
    string Reply,
    object RawResult
);
