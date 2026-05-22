using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class KrnlLoggerTests
{
    [Fact]
    public void Write_Exception_ShouldNotThrow()
    {
        var ex = Record.Exception(() => KrnlLogger.Write(new InvalidOperationException("test")));
        ex.Should().BeNull();
    }

    [Fact]
    public void Write_Message_ShouldNotThrow()
    {
        var ex = Record.Exception(() => KrnlLogger.Write("test message"));
        ex.Should().BeNull();
    }
}
