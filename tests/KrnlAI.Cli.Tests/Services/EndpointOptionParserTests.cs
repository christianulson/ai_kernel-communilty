using FluentAssertions;
using KrnlAI.Cli.Services;
using Xunit;

namespace KrnlAI.Cli.Tests.Services;

public sealed class EndpointOptionParserTests
{
    [Fact]
    public void TryResolve_NoFlag_ShouldReturnDefault()
    {
        EndpointOptionParser.TryResolve([], out var endpoint).Should().BeTrue();
        endpoint.Should().Be("http://localhost:5235");
    }

    [Fact]
    public void TryResolve_FlagWithValue_ShouldReturnValue()
    {
        EndpointOptionParser.TryResolve(["--endpoint", "http://localhost:6000"], out var endpoint)
            .Should().BeTrue();
        endpoint.Should().Be("http://localhost:6000");
    }

    [Fact]
    public void TryResolve_FlagAsLastArgument_ShouldFallbackToDefault()
    {
        EndpointOptionParser.TryResolve(["--endpoint"], out var endpoint).Should().BeTrue();
        endpoint.Should().Be("http://localhost:5235");
    }

    [Fact]
    public void TryResolve_FlagWithEquals_ShouldReturnValue()
    {
        EndpointOptionParser.TryResolve(["--endpoint=http://localhost:6000"], out var endpoint)
            .Should().BeTrue();
        endpoint.Should().Be("http://localhost:6000");
    }
}