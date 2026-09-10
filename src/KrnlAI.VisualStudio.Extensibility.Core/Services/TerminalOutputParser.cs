namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>Terminal output helpers (portable).</summary>
public static class TerminalOutputParser
{
    /// <summary>Truncates text to the given maximum length.</summary>
    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text[..maxLength] + "\n... [truncated]";
    }
}

/// <summary>Formats git diffs for chat consumption (portable).</summary>
public static class GitDiffParser
{
    /// <summary>Builds a chat-friendly summary of the given raw diff.</summary>
    public static string FormatForChat(string rawDiff)
    {
        if (string.IsNullOrWhiteSpace(rawDiff))
            return "No changes.";

        var lines = rawDiff.Split(['\n'], StringSplitOptions.None);
        var added = 0;
        var removed = 0;
        var files = new HashSet<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("+++ b/") || line.StartsWith("--- a/"))
            {
                var file = line.Substring(6);
                if (!string.IsNullOrWhiteSpace(file))
                    files.Add(file);
            }
            else if (line.StartsWith("+") && !line.StartsWith("+++"))
                added++;
            else if (line.StartsWith("-") && !line.StartsWith("---"))
                removed++;
        }

        var summary = $"{files.Count} file(s) changed, +{added}/-{removed} lines\n\n";
        return summary + "```diff\n" + TerminalOutputParser.Truncate(rawDiff, 3000) + "\n```";
    }
}