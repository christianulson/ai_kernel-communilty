using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class GitDiffParserTests
{
    [Fact]
    public void FormatForChat_EmptyDiff_ShouldReturnNoChanges()
    {
        GitDiffParser.FormatForChat("").Should().Be("No changes.");
    }

    [Fact]
    public void FormatForChat_ValidDiff_ShouldSummarizeFilesAndLines()
    {
        var diff = "diff --git a/Program.cs b/Program.cs\n--- a/Program.cs\n+++ b/Program.cs\n@@ -1 +1 @@\n-old line\n+new line";

        var result = GitDiffParser.FormatForChat(diff);

        result.Should().Contain("1 file(s) changed, +1/-1 lines");
        result.Should().Contain("```diff");
        result.Should().Contain("new line");
    }
}