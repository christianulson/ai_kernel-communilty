namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>
/// Síntese de fala (voz de saída) para o modo local/desktop.
/// Retorna WAV em bytes; implementações degradam honestamente para vazio
/// quando não há engine/vozes disponíveis.
/// </summary>
public interface ISpeechSynthesisService
{
    /// <summary>
    /// Sintetiza o texto em áudio WAV.
    /// </summary>
    /// <param name="text">Texto a falar.</param>
    /// <param name="language">Idioma preferido (opcional).</param>
    /// <param name="voice">Nome da voz (opcional).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Bytes WAV ou array vazio quando indisponível.</returns>
    Task<byte[]> SynthesizeAsync(string text, string? language = null, string? voice = null, CancellationToken ct = default);
}
