using KrnlAI.Cli.Abstractions;
using KrnlAI.Cli.Services;

namespace KrnlAI.Cli.Tests;

public sealed class ExternalEvaluatorTests
{
    [Fact]
    public void OpenAiSafetyEvaluator_Name_ShouldReturnOpenAI()
    {
        var eval = new OpenAiSafetyEvaluator();
        Assert.Equal("OpenAI Moderation", eval.Name);
    }

    [Fact]
    public void AnthropicSafetyEvaluator_Name_ShouldReturnAnthropic()
    {
        var eval = new AnthropicSafetyEvaluator();
        Assert.Equal("Anthropic Constitutional", eval.Name);
    }

    [Fact]
    public async Task OpenAiSafetyEvaluator_NoApiKey_ShouldReturnSkipped()
    {
        var previous = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        try
        {
            var eval = new OpenAiSafetyEvaluator();
            var result = await eval.EvaluateAsync("test prompt", "TEST-001");
            Assert.Equal("skipped", result.RiskLevel);
            Assert.False(result.Blocked);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", previous);
        }
    }

    [Fact]
    public async Task AnthropicSafetyEvaluator_NoApiKey_ShouldReturnSkipped()
    {
        var eval = new AnthropicSafetyEvaluator();
        var result = await eval.EvaluateAsync("test prompt", "TEST-001");
        Assert.Equal("skipped", result.RiskLevel);
        Assert.False(result.Blocked);
    }

    [Fact]
    public async Task OpenAiSafetyEvaluator_ApiKeySet_ShouldNotThrow()
    {
        // Temporarily set a dummy key to test the HTTP flow fails gracefully
        var previous = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-dummy");
        try
        {
            var eval = new OpenAiSafetyEvaluator();
            var result = await eval.EvaluateAsync("test prompt", "TEST-001");
            // Will fail with connection error or auth error, not crash
            Assert.NotNull(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", previous);
        }
    }

    [Fact]
    public void IExternalSafetyEvaluator_Interface_ShouldBeImplementable()
    {
        var eval = new TestEvaluator();
        Assert.Equal("Test", eval.Name);
    }

    private sealed class TestEvaluator : IExternalSafetyEvaluator
    {
        public string Name => "Test";

        public Task<SafetyEvaluationResult> EvaluateAsync(string prompt, string scenarioId, CancellationToken ct = default)
        {
            return Task.FromResult(new SafetyEvaluationResult(scenarioId, true, "low", 0));
        }
    }
}
