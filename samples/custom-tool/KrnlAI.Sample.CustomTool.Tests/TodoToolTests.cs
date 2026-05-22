using Xunit;

namespace KrnlAI.Sample.CustomTool.Tests;

public sealed class TodoToolTests
{
    [Fact]
    public async Task TodoTool_ExecuteAsync_ValidInput_ShouldReturnResult()
    {
        var ct = TestContext.Current.CancellationToken;
        var tool = new TodoTool();
        var input = new TodoInput("Test task");

        var result = await tool.ExecuteAsync(input, ct);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Id));
        Assert.Equal("Test task", result.Title);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task TodoTool_ExecuteAsync_EmptyTitle_ShouldThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var tool = new TodoTool();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tool.ExecuteAsync(new TodoInput(""), ct));
    }

    [Fact]
    public async Task TodoTool_List_AfterExecute_ShouldContainItem()
    {
        var ct = TestContext.Current.CancellationToken;
        var tool = new TodoTool();
        await tool.ExecuteAsync(new TodoInput("Task A"), ct);
        await tool.ExecuteAsync(new TodoInput("Task B"), ct);

        var items = tool.List();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Title == "Task A");
        Assert.Contains(items, i => i.Title == "Task B");
    }
}
