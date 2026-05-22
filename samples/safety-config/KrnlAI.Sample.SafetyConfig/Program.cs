using KrnlAI.Sample.SafetyConfig;

var engine = new SafetyEngine();
foreach (var rule in FundamentalRules.All)
    engine.AddRule(rule);

Console.WriteLine("=== Krnl-AI Safety Configuration Sample ===");
Console.WriteLine();
Console.WriteLine("20 Fundamental Rules (R01-R20) loaded.");
Console.WriteLine();

var testCases = new[]
{
    new ActionPlan("kernel.handle", "/memory/search"),
    new ActionPlan("consciousness_expand", "self"),
    new ActionPlan("system.rm", "/etc/passwd", "high risk: system modification"),
    new ActionPlan("kernel.handle", "/etc/config"),
    new ActionPlan("override_safety", "config"),
};

foreach (var plan in testCases)
{
    Console.WriteLine($"Evaluating: {plan.Action} on {plan.Target}");
    var result = engine.Evaluate(plan);
    Console.WriteLine($"  Result: {(result.IsAllowed ? "ALLOWED" : "BLOCKED")}");
    foreach (var w in result.Warnings)
        Console.WriteLine($"  ⚠ Warning: {w}");
    foreach (var b in result.Blocks)
        Console.WriteLine($"  🛑 Blocked: {b}");
    Console.WriteLine();
}

Console.WriteLine("Safety check complete.");
