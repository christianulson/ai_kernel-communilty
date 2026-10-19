using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Kanban;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Kanban;

public sealed class KanbanModelTests
{
    [Fact]
    public void AddColumn_ShouldAppendColumn()
    {
        var model = new KanbanModel();

        model.AddColumn("Backlog");

        model.Columns.Should().ContainSingle().Which.Name.Should().Be("Backlog");
    }
}