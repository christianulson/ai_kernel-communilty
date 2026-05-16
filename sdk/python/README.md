# AI Kernel Python SDK

**Cognitive agent framework with built-in safety. Zero external dependencies.**

```bash
pip install aikernel
```

```python
from aikernel import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")
response = await agent.run("analyze this dataset")
```

## Features

- **Safety by Architecture** — 20 fundamental rules (R01-R20) that cannot be overridden
- **Bio-Inspired Cognitive Cycle** — 10-step cognitive process: sense, attend, remember, evaluate, metacognize, plan, govern, execute, outcome, learn
- **Emotional State (VAD)** — Valence-Arousal-Dominance emotional modeling
- **Zero Dependencies** — Runs fully in-memory, no databases, no Docker
- **LLM Providers** — OpenAI, Anthropic, Ollama, OpenRouter, Groq
- **Streaming** — Native `async for` streaming of cognitive cycles
- **Enterprise Mode** — Connect to the C# AI Kernel backend for persistence, multi-tenancy, and scale

## Quick Start

```bash
pip install aikernel
aikernel init my-agent
cd my-agent
aikernel run --interactive
```

## Documentation

- [Quickstart (5 minutes)](docs/quickstart.md)
- [Cognitive Cycle](docs/cognitive-cycle.md)
- [Safety System](docs/safety-system.md)
- [Integrations](docs/integrations.md)
- [CLI Reference](docs/cli.md)

## License

MIT
