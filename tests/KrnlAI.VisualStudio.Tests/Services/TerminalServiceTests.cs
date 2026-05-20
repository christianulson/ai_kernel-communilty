using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class TerminalServiceTests
{
    [Fact]
    public void TerminalResult_ExitCodeZero_ShouldBeSuccess()
    {
        var result = new TerminalResult(0, "All good", "");
        result.ExitCode.Should().Be(0);
        result.Output.Should().Be("All good");
    }

    [Fact]
    public void TerminalResult_ExitCodeNonZero_ShouldBeFailure()
    {
        var result = new TerminalResult(1, "", "Error occurred");
        result.ExitCode.Should().Be(1);
        result.Error.Should().Be("Error occurred");
    }
}
