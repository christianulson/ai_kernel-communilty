namespace KrnlAI.Sample.CustomTool;

public sealed record TodoInput(string Title);

public sealed record TodoResult(string Id, string Title, bool IsComplete);

public sealed class TodoTool : ITool<TodoInput, TodoResult>
{
    private readonly List<TodoResult> _items = [];

    public string Name => "todo";
    public string Description => "Creates and manages todo items";

    public Task<TodoResult> ExecuteAsync(TodoInput input, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Title);

        var item = new TodoResult(
            Id: Guid.NewGuid().ToString("N")[..8],
            Title: input.Title,
            IsComplete: false);

        _items.Add(item);
        return Task.FromResult(item);
    }

    public IReadOnlyList<TodoResult> List() => _items.AsReadOnly();
}
