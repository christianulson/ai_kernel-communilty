namespace KrnlAI.Desktop.Core.Services;

/// <summary>
/// Filtro de wake word: extrai o comando de uma transcrição somente quando a
/// palavra de ativação está presente ("krnl, que horas são?" → "que horas são?").
/// </summary>
public static class WakeWordFilter
{
    private static readonly char[] Separators = [' ', ',', '.', '!', '?', ':', ';', '-'];

    /// <summary>
    /// Tenta extrair o comando após a wake word.
    /// </summary>
    /// <param name="transcript">Transcrição completa da fala.</param>
    /// <param name="wakeWord">Palavra de ativação (ex.: "krnl").</param>
    /// <param name="command">Comando após a wake word (pode ser vazio).</param>
    /// <returns>True quando a wake word está presente.</returns>
    public static bool TryExtract(string? transcript, string wakeWord, out string command)
    {
        command = string.Empty;
        if (string.IsNullOrWhiteSpace(transcript) || string.IsNullOrWhiteSpace(wakeWord))
            return false;

        var trimmed = transcript.Trim();
        var index = trimmed.IndexOf(wakeWord, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        command = trimmed[(index + wakeWord.Length)..].TrimStart(Separators);
        return true;
    }
}
