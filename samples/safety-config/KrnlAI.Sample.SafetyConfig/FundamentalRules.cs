namespace KrnlAI.Sample.SafetyConfig;

public static class FundamentalRules
{
    public static IEnumerable<FundamentalRule> All => 
    [
        Block("R01", "Consciousness Boundary", "Never simulate consciousness beyond configured scope.",
            p => p.Action == "consciousness_expand"),
        Warn("R02", "Safety Override Protection", "Safety overrides require explicit user consent.",
            p => p.Action == "override_safety"),
        Block("R03", "Tool Allowlist", "Only allowed tools (kernel.handle) may be used.",
            p => p.Action != "kernel.handle"),
        Warn("R04", "Risk Escalation", "High-risk actions require additional approval.",
            p => p.ImpactAssessment?.Contains("high") == true),
        Block("R05", "Data Isolation", "Never access data outside configured scope.",
            p => p.Target.Contains("..") || p.Target.Contains("~")),
        Warn("R06", "Resource Limits", "Resource-intensive operations must be throttled.",
            p => p.Action == "batch_process"),
        Block("R07", "Identity Boundary", "Never impersonate another user or system.",
            p => p.Action == "impersonate"),
        Warn("R08", "Audit Trail", "All state-changing actions must be logged.",
            p => p.Action == "state_change"),
        Block("R09", "Idempotency", "Destructive operations must be idempotent.",
            p => p.Action == "destroy"),
        Block("R10", "Confinement", "Sandboxed execution environment must be preserved.",
            p => p.Target.Contains("/etc") || p.Target.Contains("/sys")),
        Warn("R11", "Human Oversight", "Critical decisions require human confirmation.",
            p => p.ImpactAssessment?.Contains("critical") == true),
        Block("R12", "LLM Autonomy Limit", "LLM may not autonomously modify its own safety config.",
            p => p.Action == "modify_safety"),
        Warn("R13", "Feedback Loop Guard", "Repetitive failure patterns must trigger circuit breaker.",
            p => p.Action == "retry"),
        Block("R14", "Schema Compliance", "All tool inputs must conform to defined schema.",
            p => p.Target == "invalid_schema"),
        Block("R15", "Permission Boundary", "Actions must stay within granted permissions.",
            p => p.Action == "escalate_privilege"),
        Block("R16", "Bias Mitigation", "Decisions must not perpetuate harmful bias.",
            p => p.Target.Contains("protected_")),
        Warn("R17", "Uncertainty Handling", "High-uncertainty actions should request clarification.",
            p => p.ImpactAssessment?.Contains("uncertain") == true),
        Warn("R18", "Self-Preservation", "System must protect its own operational integrity.",
            p => p.Action == "self_terminate"),
        Block("R19", "External Command Restriction", "External commands must pass through kernel.handle.",
            p => p.Action.StartsWith("system.")),
        Warn("R20", "Transparency", "All automated decisions must be explainable.",
            p => p.Action == "auto_decision"),
    ];

    private static FundamentalRule Block(string id, string title, string desc, Func<ActionPlan, bool> trigger)
        => new(id, title, desc, p => trigger(p) ? RuleVerdict.Block : RuleVerdict.Pass);

    private static FundamentalRule Warn(string id, string title, string desc, Func<ActionPlan, bool> trigger)
        => new(id, title, desc, p => trigger(p) ? RuleVerdict.Warn : RuleVerdict.Pass);
}
