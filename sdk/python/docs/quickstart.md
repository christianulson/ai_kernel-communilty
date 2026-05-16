# Quickstart (5 minutes)

## Installation

```bash
pip install aikernel
```

## Basic Usage

```python
from aikernel import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")
response = await agent.run("analyze this dataset")
print(response.output)
```

## Interactive Mode

```bash
aikernel init my-agent
cd my-agent
aikernel run --interactive
```

## With LLM

Set your API key and the agent will auto-detect:

```bash
export OPENAI_API_KEY=sk-...
aikernel run "What is the capital of France?"
```

## Next Steps

- [Cognitive Cycle](cognitive-cycle.md)
- [Safety System](safety-system.md)
- [Integrations](integrations.md)
