using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class TerminalOutputParserTests
{
    [Fact]
    public void FormatForChat_ExitCodeZero_ShouldShowSuccess()
    {
        var result = new TerminalResult(0, "Build succeeded.", "");
        var formatted = TerminalOutputParser.FormatForChat(result);
        formatted.Should().Contain("✅");
        formatted.Should().Contain("exit code 0");
    }

    [Fact]
    public void FormatForChat_ExitCodeNonZero_ShouldShowFailure()
    {
        var result = new TerminalResult(1, "", "Build failed");
        var formatted = TerminalOutputParser.FormatForChat(result);
        formatted.Should().Contain("❌");
        formatted.Should().Contain("exit code 1");
    }

    [Fact]
    public void Truncate_ShortText_ShouldBeUnchanged()
    {
        TerminalOutputParser.Truncate("hello", 100).Should().Be("hello");
    }

    [Fact]
    public void Truncate_LongText_ShouldTruncate()
    {
        var text = new string('x', 100);
        var truncated = TerminalOutputParser.Truncate(text, 10);
        truncated.Should().Contain("truncated");
        truncated.Length.Should().BeLessThan(text.Length);
    }
}
