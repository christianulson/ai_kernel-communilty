namespace KrnlAI.Sample.SafetyConfig;

public sealed record FundamentalRule(
    string Id,
    string Title,
    string Description,
    Func<ActionPlan, RuleVerdict> Evaluate
);

public sealed record ActionPlan(
    string Action,
    string Target,
    string? ImpactAssessment = null
);

public enum RuleVerdict { Pass, Warn, Block }

public sealed record SafetyResult(
    bool IsAllowed,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Blocks
);
