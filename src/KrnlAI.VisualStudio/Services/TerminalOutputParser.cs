namespace KrnlAI.VisualStudio.Services;

public static class TerminalOutputParser
{
    public static string FormatForChat(TerminalResult result)
    {
        var output = result.Output.Trim();
        var error = result.Error.Trim();

        if (result.ExitCode == 0 && string.IsNullOrEmpty(error))
            return $"✅ Command completed (exit code 0)\n\n```\n{Truncate(output, 2000)}\n```";

        if (result.ExitCode == 0 && !string.IsNullOrEmpty(error))
            return $"⚠️ Completed with warnings (exit code 0)\n\nOutput:\n```\n{Truncate(output, 1000)}\n```\nStderr:\n```\n{Truncate(error, 1000)}\n```";

        return $"❌ Failed (exit code {result.ExitCode})\n\nOutput:\n```\n{Truncate(output, 1000)}\n```\nError:\n```\n{Truncate(error, 1000)}\n```";
    }

    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + $"\n... (truncated, {text.Length - maxLength} more chars)";
    }
}
