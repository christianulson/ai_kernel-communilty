# Krnl-AI Community

> A cognitive runtime for building local-first agents with persistent memory, safety checks, evolving skills, developer tooling, and desktop P2P surfaces.

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
- Predictive world models for simulation and planning
- Causal reasoning for understanding cause-effect relationships
- Continuous learning pipeline (memory → analysis → simulation → consolidation)
- Editor integrations for VS Code and Visual Studio
- Cross-platform desktop apps via WPF and Tauri
- Peer-to-peer video sessions via WebRTC signaling

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
| [Getting Started](01-Getting-Started/getting-started.md) | Install, configure, first run |
| [CLI Reference](02-CLI/cli-reference.md) | All CLI commands and options |
| [Architecture](03-Architecture/architecture.md) | System design and principles |
| [Cognitive Cycle](04-Cognitive-Cycle/cognitive-cycle.md) | 10-step cognitive processing pipeline |
| [Memory System](05-Memory/memory-system.md) | Episodic, semantic, procedural, working, emotional, autobiographical, and prospective memory |
| [Safety System](06-Safety/safety-system.md) | 20 fundamental rules, adversarial guard, ethics |
| [Emotion System](07-Emotions/emotion-system.md) | VAD model, pain/reward, emotional memory |
| [SDK Guide](08-SDK/sdk-guide.md) | Python and .NET SDK documentation |
| [Sidecar API](09-API/sidecar-api.md) | HTTP API endpoints reference and P2P signaling |
| [Desktop Apps](10-Desktop/desktop-apps.md) | WPF and Tauri desktop applications, API keys, privacy, and P2P |
| [Editor Extensions](11-Editors/editor-extensions.md) | VS Code and Visual Studio extensions |
| [Samples](12-Samples/samples.md) | Example projects and patterns |
| [Integrations](13-Integrations/integrations.md) | LangChain, CrewAI, AutoGen, FastAPI |
| [Contributing](14-Contributing/contributing.md) | How to contribute to the project |
| [Comparative Matrix](comparative-matrix.md) | Krnl-AI vs 15 market alternatives — feature-by-feature comparison |

## Community

- **GitHub Issues** — Bug reports and feature requests
- **GitHub Discussions** — Q&A, ideas, and showcase (when enabled)
- **License** — MIT
