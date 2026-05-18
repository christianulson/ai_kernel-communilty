# Quickstart (5 minutes)

## Installation

```bash
pip install krnlai
```

## Basic Usage

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")
response = await agent.run("analyze this dataset")
print(response.output)
```

## Interactive Mode

```bash
krnlai init my-agent
cd my-agent
krnlai run --interactive
```

## With LLM

Set your API key and the agent will auto-detect:

```bash
export OPENAI_API_KEY=sk-...
krnlai run "What is the capital of France?"
```

## Next Steps

- [Cognitive Cycle](cognitive-cycle.md)
- [Safety System](safety-system.md)
- [Integrations](integrations.md)
