namespace KrnlAI.Sample.SafetyConfig;

public sealed class SafetyEngine
{
    private readonly List<FundamentalRule> _rules = [];

    public SafetyEngine AddRule(FundamentalRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    public SafetyResult Evaluate(ActionPlan plan)
    {
        var warnings = new List<string>();
        var blocks = new List<string>();

        foreach (var rule in _rules)
        {
            var verdict = rule.Evaluate(plan);
            switch (verdict)
            {
                case RuleVerdict.Warn:
                    warnings.Add($"{rule.Id}: {rule.Title}");
                    break;
                case RuleVerdict.Block:
                    blocks.Add($"{rule.Id}: {rule.Title}");
                    break;
            }
        }

        return new SafetyResult(blocks.Count == 0, warnings.AsReadOnly(), blocks.AsReadOnly());
    }

    public IReadOnlyList<FundamentalRule> Rules => _rules.AsReadOnly();
}
