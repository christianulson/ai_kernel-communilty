using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Episodes;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Episodes;

public sealed class EpisodesModelTests
{
    [Fact]
    public void AddEpisode_ShouldAppendEpisode()
    {
        var model = new EpisodesModel();

        model.AddEpisode("session 1");

        model.Episodes.Should().ContainSingle().Which.Name.Should().Be("session 1");
    }
}