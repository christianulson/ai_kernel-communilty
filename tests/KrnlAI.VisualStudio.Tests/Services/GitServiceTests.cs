using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class GitServiceTests
{
    [Fact]
    public void GitDiffParser_FormatForChat_WithDiff_ShouldShowSummary()
    {
        var diff = @"--- a/file1.cs
+++ b/file1.cs
-var x = 1;
+var x = 2;
--- a/file2.cs
+++ b/file2.cs
+new line";
        var formatted = GitDiffParser.FormatForChat(diff);
        formatted.Should().Contain("file(s) changed");
        formatted.Should().Contain("+");
        formatted.Should().Contain("-");
    }

    [Fact]
    public void GitDiffParser_FormatForChat_WithEmpty_ShouldReturnNoChanges()
    {
        GitDiffParser.FormatForChat("").Should().Contain("No changes");
        GitDiffParser.FormatForChat(null!).Should().Contain("No changes");
    }
}
