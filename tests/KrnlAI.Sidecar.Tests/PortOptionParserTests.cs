using FluentAssertions;
using KrnlAI.Sidecar;
using Xunit;

namespace KrnlAI.Sidecar.Tests;

public sealed class PortOptionParserTests
{
    [Fact]
    public void Resolve_NoArgs_ShouldReturnDefault()
    {
        PortOptionParser.Resolve([]).Should().Be(5001);
    }

    [Fact]
    public void Resolve_FlagWithValue_ShouldReturnValue()
    {
        PortOptionParser.Resolve(["--port", "5010"]).Should().Be(5010);
    }

    [Fact]
    public void Resolve_FlagAsLastArgument_ShouldReturnDefault()
    {
        PortOptionParser.Resolve(["--port"]).Should().Be(5001);
    }

    [Fact]
    public void Resolve_FlagWithEquals_ShouldReturnValue()
    {
        PortOptionParser.Resolve(["--port=5020"]).Should().Be(5020);
    }

    [Fact]
    public void Resolve_InvalidPort_ShouldReturnDefault()
    {
        PortOptionParser.Resolve(["--port", "99999"]).Should().Be(5001);
        PortOptionParser.Resolve(["--port", "0"]).Should().Be(5001);
        PortOptionParser.Resolve(["--port", "abc"]).Should().Be(5001);
    }

    [Fact]
    public void Resolve_ConfigValue_ShouldBeUsedWhenNoFlag()
    {
        PortOptionParser.Resolve([], configValue: "5050").Should().Be(5050);
    }
}