# Samples

The Community repository includes sample projects to demonstrate common patterns and use cases.

## Hello Agent

The simplest possible Krnl-AI workflow:

```bash
dotnet tool install -g KrnlAI.Cli
krnlai chat --local
```

Try this conversation:

```
> Remember that this project uses local-first memory.
> What do you remember about this project?
```

The first prompt writes memory through the embedded kernel. The second prompt retrieves it through the local runtime.

Source: `samples/hello-agent/`

## Custom Tool

Demonstrates the pattern for creating custom community tools:

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

### Tool Design Principles

1. Define the tool input schema
2. Validate the request before execution
3. Run safety checks for actions with side effects
4. Return structured output to the agent

Source: `samples/custom-tool/`

## Python Examples

The Python SDK includes several example scripts:

### Basic Agent (`examples/01-basic-agent/`)

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
result = await agent.run("Hello, world!")
print(result.output)
```

### Customer Support (`examples/02-customer-support/`)

A multi-turn customer support agent with memory persistence.

### Research Assistant (`examples/03-research-assistant/`)

An agent that searches semantic memory and provides research summaries.

### Multi-Agent (`examples/04-multi-agent/`)

Two agents communicating through shared memory.

### Enterprise Mode (`examples/05-enterprise-mode/`)

Connecting the Python SDK to the C# enterprise backend.

## Template Projects

Initialize reusable templates via the CLI:

```bash
krnlai new agent my-agent       # Basic cognitive agent
krnlai new tool my-tool          # Custom tool
krnlai new policy my-policy      # Policy template
krnlai new cognitive-cycle       # Custom cognitive cycle
```
