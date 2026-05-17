# Quickstart

This guide starts a local AI Kernel session from a clean machine.

## Prerequisites

- .NET SDK
- An LLM provider key or a local OpenAI-compatible endpoint

## Install the CLI

```bash
dotnet tool install -g AIKernel.Cli
```

## Configure a Provider

```bash
aikernel config set provider openai
aikernel config set api_key sk-...
```

For local models:

```bash
aikernel config set provider openai-compatible
aikernel config set endpoint http://localhost:11434/v1
aikernel config set model llama3.1
```

## Start a Local Session

```bash
aikernel chat --local
```

The local mode uses the embedded kernel and local storage. It does not require the
enterprise API services.

## Run the Sidecar

```bash
aikernel serve --local --port 5117
```

The sidecar exposes local endpoints for editor integrations and automation.
