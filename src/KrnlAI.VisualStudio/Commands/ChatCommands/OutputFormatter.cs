namespace KrnlAI.VisualStudio.Commands.ChatCommands;

public static class OutputFormatter
{
    public static string AsCodeBlock(string text, string? language = null)
    {
        var lang = language ?? "";
        return $"```{lang}\n{text}\n```";
    }

    public static string AsSuccess(string message)
    {
        return $"✅ {message}";
    }

    public static string AsError(string message)
    {
        return $"❌ {message}";
    }

    public static string AsWarning(string message)
    {
        return $"⚠️ {message}";
    }
}
