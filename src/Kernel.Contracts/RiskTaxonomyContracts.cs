namespace Kernel.Contracts;

/// <summary>
/// Canonical risk levels used by plans, tools and governance checks.
/// </summary>
public enum RiskTaxonomyLevel
{
    /// <summary>
    /// Low operational or safety risk.
    /// </summary>
    Low,

    /// <summary>
    /// Medium operational or safety risk.
    /// </summary>
    Medium,

    /// <summary>
    /// High operational or safety risk; usually requires additional approval.
    /// </summary>
    High
}

/// <summary>
/// Shared taxonomy helpers for validating textual risk levels.
/// </summary>
public static class RiskTaxonomy
{
    /// <summary>
    /// Parses a textual risk level using the canonical taxonomy.
    /// </summary>
    public static bool TryParse(string? value, out RiskTaxonomyLevel level)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "low":
                level = RiskTaxonomyLevel.Low;
                return true;
            case "medium":
                level = RiskTaxonomyLevel.Medium;
                return true;
            case "high":
                level = RiskTaxonomyLevel.High;
                return true;
            default:
                level = default;
                return false;
        }
    }

    /// <summary>
    /// Returns true when the textual risk level is high.
    /// </summary>
    public static bool IsHigh(string? value)
        => TryParse(value, out var level) && level == RiskTaxonomyLevel.High;

    /// <summary>
    /// Returns true when the textual risk level is medium.
    /// </summary>
    public static bool IsMedium(string? value)
        => TryParse(value, out var level) && level == RiskTaxonomyLevel.Medium;
}
