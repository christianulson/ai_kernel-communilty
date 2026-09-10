using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Backlog;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Backlog;

public sealed class BacklogModelTests
{
    [Fact]
    public void AddItem_ShouldAppendAndReturnIndex()
    {
        var model = new BacklogModel();

        var index = model.AddItem("fix bug");

        index.Should().Be(0);
        model.Items.Should().ContainSingle().Which.Title.Should().Be("fix bug");
    }
}