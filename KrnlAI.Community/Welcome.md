# Krnl-AI Community

> A cognitive runtime for building local-first agents with persistent memory, safety checks, evolving skills, and developer tooling.

Krnl-AI Community is the open, local-first edition of Krnl-AI. It runs entirely on your machine without requiring hosted infrastructure. All state is stored locally via SQLite, and you can bring your own LLM provider (OpenAI, Ollama, Anthropic, etc.).

## Quick Start

```bash
dotnet tool install -g KrnlAI.Cli
krnlai config set provider ollama
krnlai chat --local
```

## What You Can Build

- Local agents with your preferred LLM
- Persistent memory backed by SQLite with semantic search
- Skills that can be created, refined, exported, and installed
- Safety-aware execution with 20 fundamental rules (R01-R20)
- Editor integrations for VS Code and Visual Studio
- Cross-platform desktop apps via WPF and Tauri

## Packages

| Package | Purpose |
|---------|---------|
| `KrnlAI.Cli` | Local CLI and interactive TUI |
| `KrnlAI.Sidecar` | Local HTTP sidecar for tools and editors |
| `KrnlAI.Contracts` | Public DTOs and interfaces |
| `KrnlAI.Embedded` | In-process kernel for community apps |

## SDKs

| SDK | Language | Source |
|-----|----------|--------|
| Python SDK | Python 3.10+ | `sdk/python/` |
| .NET SDK | netstandard2.0 | `sdk/dotnet/` |

## Documentation Map

| Section | Description |
|---------|-------------|
| [[01-Getting-Started/index\|Getting Started]] | Install, configure, first run |
| [[02-CLI/index\|CLI Reference]] | All CLI commands and options |
| [[03-Architecture/index\|Architecture]] | System design and principles |
| [[04-Cognitive-Cycle/index\|Cognitive Cycle]] | 10-step cognitive processing pipeline |
| [[05-Memory/index\|Memory System]] | Episodic, semantic, working, emotional memory |
| [[06-Safety/index\|Safety System]] | 20 fundamental rules, adversarial guard, ethics |
| [[07-Emotions/index\|Emotion System]] | VAD model, pain/reward, emotional memory |
| [[08-SDK/index\|SDK Guide]] | Python and .NET SDK documentation |
| [[09-API/index\|Sidecar API]] | HTTP API endpoints reference |
| [[10-Desktop/index\|Desktop Apps]] | WPF and Tauri desktop applications |
| [[11-Editors/index\|Editor Extensions]] | VS Code and Visual Studio extensions |
| [[12-Samples/index\|Samples]] | Example projects and patterns |
| [[13-Integrations/index\|Integrations]] | LangChain, CrewAI, AutoGen, FastAPI |
| [[14-Contributing/index\|Contributing]] | How to contribute to the project |

## Community

- **GitHub Issues** — Bug reports and feature requests
- **GitHub Discussions** — Q&A, ideas, and showcase (when enabled)
- **License** — MIT
