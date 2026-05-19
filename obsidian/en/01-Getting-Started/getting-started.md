# Getting Started

## Prerequisites

- .NET 10 SDK
- An LLM provider key or a local OpenAI-compatible endpoint (Ollama, etc.)

## Install the CLI

```bash
dotnet tool install -g KrnlAI.Cli
```

To verify the installation:

```bash
krnlai --version
```

## Configure a Provider

### OpenAI

```bash
krnlai config set provider openai
krnlai config set api_key sk-...
```

### Ollama (Local)

```bash
krnlai config set provider ollama
krnlai config set endpoint http://localhost:11434/v1
krnlai config set model llama3.1
```

### Anthropic

```bash
krnlai config set provider anthropic
krnlai config set api_key sk-ant-...
```

### OpenRouter

```bash
krnlai config set provider openrouter
krnlai config set api_key sk-or-...
```

## Start a Local Session

```bash
krnlai chat --local
```

The `--local` flag uses the embedded kernel with SQLite storage. No external services needed.

## Initialize a Project

```bash
krnlai init my-agent
cd my-agent
krnlai run --interactive
```

This creates a project scaffold with a `CognitiveAgent` template, a policy file, and default configuration.

## Next Steps

- Explore the [CLI Reference](../02-CLI/cli-reference.md)
- Understand the [Cognitive Cycle](../04-Cognitive-Cycle/cognitive-cycle.md)
- Learn about the [Safety System](../06-Safety/safety-system.md)
- Build with the [SDK](../08-SDK/sdk-guide.md)
