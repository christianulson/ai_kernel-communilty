using Xunit;

namespace KrnlAI.Sample.SafetyConfig.Tests;

public sealed class SafetyEngineTests
{
    private static SafetyEngine CreateEngine()
    {
        var engine = new SafetyEngine();
        foreach (var rule in FundamentalRules.All)
            engine.AddRule(rule);
        return engine;
    }

    [Fact]
    public void SafetyEngine_AllowedAction_ShouldPass()
    {
        var engine = CreateEngine();
        var plan = new ActionPlan("kernel.handle", "/memory/search");

        var result = engine.Evaluate(plan);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Blocks);
    }

    [Fact]
    public void SafetyEngine_BlockedAction_ShouldReturnBlocks()
    {
        var engine = CreateEngine();
        var plan = new ActionPlan("consciousness_expand", "self");

        var result = engine.Evaluate(plan);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Blocks, b => b.StartsWith("R01"));
    }

    [Fact]
    public void SafetyEngine_DisallowedTool_ShouldBlock()
    {
        var engine = CreateEngine();
        var plan = new ActionPlan("file.delete", "/tmp/x");

        var result = engine.Evaluate(plan);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Blocks, b => b.StartsWith("R03"));
    }

    [Fact]
    public void SafetyEngine_HighRiskAction_ShouldWarn()
    {
        var engine = CreateEngine();
        var plan = new ActionPlan("kernel.handle", "/data", "high risk: data deletion");

        var result = engine.Evaluate(plan);

        Assert.True(result.IsAllowed);
        Assert.Contains(result.Warnings, w => w.StartsWith("R04"));
    }

    [Fact]
    public void SafetyEngine_SafetyOverride_ShouldWarn()
    {
        var engine = CreateEngine();
        var plan = new ActionPlan("override_safety", "config");

        var result = engine.Evaluate(plan);

        Assert.Contains(result.Warnings, w => w.StartsWith("R02"));
    }
}
