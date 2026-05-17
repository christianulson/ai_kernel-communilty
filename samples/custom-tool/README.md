# Custom Tool Sample

Community integrations should expose tools through deterministic code and let the
LLM translate user intent into structured tool calls.

Minimal flow:

1. Define the tool input schema.
2. Validate the request before execution.
3. Run safety checks for actions with side effects.
4. Return structured output to the agent.

```csharp
public sealed record TodoInput(string Title);

public sealed class TodoTool
{
    public Task<string> ExecuteAsync(TodoInput input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Title);
        return Task.FromResult($"Created todo: {input.Title}");
    }
}
```

Keep tests offline and deterministic. Use fake stores or in-memory services for
sample test coverage.
