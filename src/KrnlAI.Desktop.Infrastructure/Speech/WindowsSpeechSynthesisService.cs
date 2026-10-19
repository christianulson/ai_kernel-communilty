using System.Speech.Synthesis;
using KrnlAI.Desktop.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.Infrastructure.Speech;

/// <summary>
/// Síntese de fala via Windows SAPI (System.Speech).
/// Gera WAV em memória; sem vozes instaladas retorna vazio (falha honesta).
/// </summary>
public sealed class WindowsSpeechSynthesisService(
    ILogger<WindowsSpeechSynthesisService>? logger = null) : ISpeechSynthesisService
{
    /// <inheritdoc />
    public Task<byte[]> SynthesizeAsync(
        string text,
        string? language = null,
        string? voice = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Array.Empty<byte>());

        return Task.Run(() => Synthesize(text, voice), ct);
    }

    private byte[] Synthesize(string text, string? voice)
    {
        try
        {
            using var synthesizer = new SpeechSynthesizer();

            if (!string.IsNullOrWhiteSpace(voice))
            {
                try
                {
                    synthesizer.SelectVoice(voice);
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Voice {Voice} unavailable; using default voice", voice);
                }
            }

            using var stream = new MemoryStream();
            synthesizer.SetOutputToWaveStream(stream);
            synthesizer.Speak(text);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Windows speech synthesis failed");
            return Array.Empty<byte>();
        }
    }
}
