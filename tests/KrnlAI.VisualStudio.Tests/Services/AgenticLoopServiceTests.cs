using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class AgenticLoopServiceTests
{
    [Fact]
    public void AgenticLoopResult_Completed_ShouldHaveSummary()
    {
        var result = new AgenticLoopResult("Completed", "Task done", null, null);
        result.Status.Should().Be("Completed");
        result.Summary.Should().Be("Task done");
    }

    [Fact]
    public void AgenticLoopResult_Failed_ShouldHaveError()
    {
        var result = new AgenticLoopResult("Failed", null, "Something broke", null);
        result.Status.Should().Be("Failed");
        result.Error.Should().Be("Something broke");
    }

    [Fact]
    public void AgenticLoopResult_Cancelled_ShouldHaveStatus()
    {
        var result = new AgenticLoopResult("Cancelled", null, null, null);
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public void AgentStep_ShouldStoreValues()
    {
        var step = new AgentStep(1, "Analyzing", "Done", true);
        step.Number.Should().Be(1);
        step.Description.Should().Be("Analyzing");
        step.Result.Should().Be("Done");
        step.IsCompleted.Should().BeTrue();
    }
}
