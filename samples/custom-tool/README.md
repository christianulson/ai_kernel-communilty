# Custom Tool Sample

Shows how to create a custom tool for Krnl-AI using a deterministic code pattern.

## Flow

1. Define the tool input schema (record/class)
2. Implement the `ITool<TInput, TOutput>` interface
3. Register and execute from your application
4. Keep tests offline, deterministic — use fake stores or in-memory services

## Usage

```bash
dotnet run --project KrnlAI.Sample.CustomTool
```

## Code

```csharp
public sealed record TodoInput(string Title);

public sealed class TodoTool : ITool<TodoInput, TodoResult>
{
    public string Name => "todo";
    public string Description => "Creates and manages todo items";

    public Task<TodoResult> ExecuteAsync(TodoInput input, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Title);
        // ...
    }
}
```

## Tests

```bash
dotnet test KrnlAI.Sample.CustomTool.Tests
```
