# Quickstart

This guide starts a local Krnl-AI session from a clean machine.

## Prerequisites

- .NET SDK
- An LLM provider key or a local OpenAI-compatible endpoint

## Install the CLI

```bash
dotnet tool install -g KrnlAI.Cli
```

## Configure a Provider

```bash
krnlai config set provider openai
krnlai config set api_key sk-...
```

For local models:

```bash
krnlai config set provider openai-compatible
krnlai config set endpoint http://localhost:11434/v1
krnlai config set model llama3.1
```

## Start a Local Session

```bash
krnlai chat --local
```

The local mode uses the embedded kernel and local storage. It does not require the
enterprise API services.

## Run the Sidecar

```bash
krnlai serve --local --port 5117
```

The sidecar exposes local endpoints for editor integrations and automation.
