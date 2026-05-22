# Hello Agent Sample

Minimal example showing how to create and run a Krnl-AI agent using the SDK client.

## Usage (SDK)

```bash
dotnet run --project KrnlAI.Sample.HelloAgent
```

Optionally specify kernel URL and goal:

```bash
dotnet run --project KrnlAI.Sample.HelloAgent -- http://localhost:5000 "Create a reminder"
```

## Usage (CLI)

```bash
dotnet tool install -g KrnlAI.Cli
krnlai chat --local
```

Then try:
```
Remember that this project uses local-first memory.
What do you remember about this project?
```

The first prompt writes memory through the embedded kernel.
The second prompt retrieves it through the local runtime.

## Tests

```bash
dotnet test KrnlAI.Sample.HelloAgent.Tests
```
