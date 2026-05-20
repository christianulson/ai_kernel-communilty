using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class ApplyEditServiceTests
{
    [Fact]
    public void ApplyEditResult_Approved_ShouldBeTrue()
    {
        var result = new ApplyEditResult(true, "diff", null);
        result.Approved.Should().BeTrue();
        result.Diff.Should().Be("diff");
    }

    [Fact]
    public void ApplyEditResult_Rejected_ShouldHaveError()
    {
        var result = new ApplyEditResult(false, null, "Cancelled");
        result.Approved.Should().BeFalse();
        result.Error.Should().Be("Cancelled");
    }
}
