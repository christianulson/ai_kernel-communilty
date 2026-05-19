# Krnl-AI Community

Krnl-AI Community is the open-source, local-first edition of Krnl-AI — a cognitive runtime
for building intelligent agents with persistent memory, safety checks, evolving skills,
and developer tooling. It runs entirely on your machine without requiring hosted infrastructure.

## Features

### Cognitive Cycle (10 Steps)

A structured processing pipeline inspired by human cognition:

```
Sensor → Attention → Memory → Evaluation → Metacognition
→ Planning → Governance → Execution → Outcome → Learning
```

Progresses through 4 phases: `PERCEPTION → DELIBERATION → ACTION → REFLECTION`

### Memory System (5 Types)

| Memory | Purpose |
|--------|---------|
| **Working** | Immediate context (TTL-based eviction) |
| **Episodic** | Past execution history with LRU pruning |
| **Semantic** | Factual knowledge with vector search |
| **Emotional** | Emotional state transitions over time |
| **Procedural** | How-to knowledge and learned behavior |

### Safety & Guardrails (20 Rules)

Multi-layered safety with 20 fundamental rules (R01-R20), adversarial guard,
ethical enforcer, rate limiting, and risk scoring — every action validated before execution.

### Emotional Model (VAD)

Valence-Arousal-Dominance dimensional model that influences decision-making,
with pain/reward learning signals and natural emotional decay.

### Multi-Provider LLM Support

Bring your own LLM: OpenAI, Ollama, Anthropic, OpenRouter, DeepSeek,
Google Gemini, Groq, or any OpenAI-compatible endpoint.

### SDKs

- **Python SDK** (`sdk/python/`) — Full cognitive cycle access
- **.NET SDK** (`sdk/dotnet/`) — Native C# integration

### Editor Integrations

- **VS Code Extension** — Chat panel, inline completions, agent mode, memory browser
- **Visual Studio Extension** — Tool window, code analysis, chat history

### Desktop Apps

- **WPF** (Windows) — Full-featured desktop with dashboard, memory browser, policy editor
- **Tauri** (Cross-platform) — Chat interface, system tray, native notifications

---

## Quick Start

```bash
dotnet tool install -g KrnlAI.Cli
krnlai config set provider ollama
krnlai config set endpoint http://localhost:11434/v1
krnlai chat --local
```

## Packages

| Package | Purpose | License |
| --- | --- | --- |
| `KrnlAI.Cli` | Local CLI and interactive TUI | MIT |
| `KrnlAI.Sidecar` | Local HTTP sidecar for tools and editors | MIT |
| `KrnlAI.Embedded` | In-process kernel for community apps | MIT |

## Documentation

- [Wiki (English)](WikiObsidian/Home.md)
- [Obsidian Vault — EN](obsidian/en/)
- [Obsidian Vault — PT-BR](obsidian/pt-br/)
- [Quickstart](docs/quickstart.md)
- [Architecture](docs/architecture.md)
- [CLI Guide](docs/getting-started-cli.md)
- [Contributing](CONTRIBUTING.md)

## Community

Use GitHub Issues for bug reports and feature requests. GitHub Discussions should
be used for Q&A, ideas, and showcase posts once enabled in the public repository.

## License

MIT. See [LICENSE](LICENSE).
