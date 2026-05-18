# API Reference

## CognitiveAgent

Primary interface for the Krnl-AI.

```python
agent = CognitiveAgent(
    mode="auto",          # "auto" | "standalone" | "enterprise"
    safety_level="strict",
    endpoint="",          # C# backend URL (enterprise mode)
    api_key="",           # C# backend API key
    max_iterations=10,
    enable_emotions=True,
)
```

### Methods

- `run(command, context=None)` → `ResultEnvelope`
- `run_command(envelope)` → `ResultEnvelope`
- `stream(command)` → `AsyncGenerator[CycleEvent]`
- `close()` → Cleanup resources

## SafetyChecker

```python
checker = SafetyChecker()
verdict = checker.evaluate_all(context)
# verdict.allowed, verdict.risk_score, verdict.blocked_by
```

## LLM Providers

```python
from krnlai.llm.openai import OpenAIProvider
from krnlai.llm.anthropic import AnthropicProvider
from krnlai.llm.google import GoogleProvider
from krnlai.llm.groq import GroqProvider
from krnlai.llm.deepseek import DeepSeekProvider
from krnlai.llm.ollama import OllamaProvider
from krnlai.llm.openrouter import OpenRouterProvider
```
