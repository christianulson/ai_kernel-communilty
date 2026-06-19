namespace KrnlAI.VisualStudio.Services;

public static class GitDiffParser
{
    public static string FormatForChat(string rawDiff)
    {
        if (string.IsNullOrWhiteSpace(rawDiff))
            return "No changes.";

        var lines = rawDiff.Split(['\n'], StringSplitOptions.None);
        var added = 0;
        var removed = 0;
        var files = new System.Collections.Generic.HashSet<string>();

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

        var summary = $"📄 {files.Count} file(s) changed, +{added}/-{removed} lines\n\n";
        return summary + "```diff\n" + TerminalOutputParser.Truncate(rawDiff, 3000) + "\n```";
    }
}
