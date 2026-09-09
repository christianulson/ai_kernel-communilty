namespace KrnlAI.VisualStudio.Extensibility.Core.CodeLens;

/// <summary>
/// Decides whether a code element should receive the "Explain this code" CodeLens.
/// </summary>
public static class ExplainCodeLensPolicy
{
    /// <summary>Label shown on the CodeLens adornment.</summary>
    public const string Label = "Krnl-AI: Explain this code";

    private static readonly HashSet<string> SupportedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "class",
        "struct",
        "method"
    };

    /// <summary>
    /// Returns whether the given code element kind is supported by the explain CodeLens.
    /// </summary>
    public static bool ShouldProvide(string? kind)
    {
        return !string.IsNullOrWhiteSpace(kind) && SupportedKinds.Contains(kind);
    }
}