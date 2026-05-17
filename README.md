# AI Kernel Community

AI Kernel Community is the local-first edition of AI Kernel: a cognitive runtime for
agents with persistent memory, safety checks, evolving skills, and a developer CLI.
It is designed to run on your machine without requiring hosted infrastructure.

```bash
dotnet tool install -g AIKernel.Cli
aikernel chat --local
```

## What You Can Build

- Local agents that work with your preferred LLM provider
- Persistent memory backed by SQLite
- Semantic search over local memory
- Skills that can be created, refined, exported, and installed
- Safety-aware execution with policy checks before actions
- Local sidecar APIs for editors and tools

## Packages

| Package | Purpose | License |
| --- | --- | --- |
| `Kernel.Contracts` | Public DTOs and interfaces | MIT |
| `AIKernel.Cli` | Local CLI and interactive TUI | MIT |
| `AIKernel.Sidecar` | Local HTTP sidecar for tools | MIT |
| `AIKernel.Embedded` | In-process kernel for community apps | MIT |

## Quick Start

```bash
dotnet tool install -g AIKernel.Cli
aikernel config set provider openai
aikernel config set api_key sk-...
aikernel chat --local
```

For offline or local-model workflows, configure an OpenAI-compatible endpoint such
as Ollama or another local gateway.

## Documentation

- [Quickstart](docs/quickstart.md)
- [Architecture](docs/architecture.md)
- [CLI guide](docs/getting-started-cli.md)
- [Contributing](CONTRIBUTING.md)

## Community

Use GitHub Issues for bug reports and feature requests. GitHub Discussions should
be used for Q&A, ideas, and showcase posts once enabled in the public repository.

## License

MIT. See [LICENSE](LICENSE).
