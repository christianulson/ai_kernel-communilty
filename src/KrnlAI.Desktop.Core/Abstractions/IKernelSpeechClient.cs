namespace KrnlAI.Desktop.Core.Abstractions;

/// <summary>Cliente focado em transcrição e síntese de áudio.</summary>
public interface IKernelSpeechClient
{
    Task<byte[]> GenerateSpeechAsync(string text, string? language = null, string? voice = null, CancellationToken cancellationToken = default);
    Task<string?> TranscribeAudioAsync(byte[] audioData, CancellationToken cancellationToken = default);
}
