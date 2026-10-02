using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class WakeWordFilterTests
{
    [Fact]
    public void TryExtract_PresentWithCommand_ShouldReturnCommand()
    {
        var found = WakeWordFilter.TryExtract("krnl, que horas são?", "krnl", out var command);

        Assert.True(found);
        Assert.Equal("que horas são?", command);
    }

    [Fact]
    public void TryExtract_CaseInsensitive_ShouldMatch()
    {
        var found = WakeWordFilter.TryExtract("KRNL ligue a luz", "krnl", out var command);

        Assert.True(found);
        Assert.Equal("ligue a luz", command);
    }

    [Fact]
    public void TryExtract_PresentWithoutCommand_ShouldReturnEmptyCommand()
    {
        var found = WakeWordFilter.TryExtract("Krnl!", "krnl", out var command);

        Assert.True(found);
        Assert.Equal(string.Empty, command);
    }

    [Fact]
    public void TryExtract_Absent_ShouldReturnFalse()
    {
        var found = WakeWordFilter.TryExtract("que horas são?", "krnl", out var command);

        Assert.False(found);
        Assert.Equal(string.Empty, command);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryExtract_EmptyTranscript_ShouldReturnFalse(string? transcript)
    {
        Assert.False(WakeWordFilter.TryExtract(transcript, "krnl", out _));
    }

    [Fact]
    public void TryExtract_EmptyWakeWord_ShouldReturnFalse()
    {
        Assert.False(WakeWordFilter.TryExtract("krnl algo", "  ", out _));
    }
}
