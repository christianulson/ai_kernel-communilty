using System.IO;

namespace KrnlAI.VisualStudio.Extensibility.Core.Prompts;

/// <summary>
/// Builds prompts sent to the Krnl-AI chat.
/// </summary>
public static class AnalyzePromptBuilder
{
    /// <summary>Builds the prompt used by "Send selection to chat".</summary>
    public static string BuildCodePrompt(string language, string filePath, string code)
    {
        var fileName = Path.GetFileName(filePath);
        return $"Analyze this {language} from {fileName}:\n\n```{language}\n{code}\n```";
    }

    /// <summary>Builds the prompt used by "Analyze error".</summary>
    public static string BuildErrorPrompt(string message, string? file, int line)
    {
        var fileName = string.IsNullOrWhiteSpace(file) ? "unknown" : Path.GetFileName(file);
        return $"Analyze this build error and suggest a fix:\n\nError: {message}\nFile: {fileName}\nLine: {line}";
    }
}