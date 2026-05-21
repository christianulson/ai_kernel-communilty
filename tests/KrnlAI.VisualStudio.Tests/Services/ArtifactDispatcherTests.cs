using KrnlAI.VisualStudio.ToolWindows.Chat.Artifacts;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class ArtifactDispatcherTests
{
    [Fact]
    public void DetectArtifacts_MarkdownBlock_ShouldReturnMarkdownType()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "Here is some markdown:\n```artifact markdown\n# Hello\nWorld\n```";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().HaveCount(1);
        artifacts[0].Type.Should().Be(ArtifactType.Markdown);
        artifacts[0].Content.Should().Be("# Hello\nWorld");
    }

    [Fact]
    public void DetectArtifacts_TableBlock_ShouldReturnTableType()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "```table\n| A | B |\n| 1 | 2 |\n```";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().HaveCount(1);
        artifacts[0].Type.Should().Be(ArtifactType.Table);
    }

    [Fact]
    public void DetectArtifacts_MermaidBlock_ShouldReturnMermaidType()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "```mermaid\nflowchart LR\nA-->B\n```";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().HaveCount(1);
        artifacts[0].Type.Should().Be(ArtifactType.Mermaid);
    }

    [Fact]
    public void DetectArtifacts_ChartBlock_ShouldReturnChartType()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "```chart\nSales|100\nCosts|60\n```";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().HaveCount(1);
        artifacts[0].Type.Should().Be(ArtifactType.Chart);
    }

    [Fact]
    public void StripArtifactBlocks_ShouldRemoveAllBlocks()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "Hello\n```artifact markdown\n# Test\n```\nWorld\n```table\n|1|2|\n```";

        var stripped = dispatcher.StripArtifactBlocks(text);

        stripped.Should().NotContain("```artifact");
        stripped.Should().NotContain("```table");
        stripped.Should().Contain("Hello");
        stripped.Should().Contain("World");
    }

    [Fact]
    public void DetectArtifacts_MultipleBlocks_ShouldReturnAll()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "```mermaid\ngraph\n```\nmore text\n```table\n|a|b|\n```";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().HaveCount(2);
        artifacts[0].Type.Should().Be(ArtifactType.Mermaid);
        artifacts[1].Type.Should().Be(ArtifactType.Table);
    }

    [Fact]
    public void DetectArtifacts_NoBlocks_ShouldReturnEmpty()
    {
        var dispatcher = new ArtifactDispatcher();
        var text = "Just plain text with no artifact blocks.";

        var artifacts = dispatcher.DetectArtifacts(text);

        artifacts.Should().BeEmpty();
    }


}
