# API Reference

## CognitiveAgent

Primary interface for the AI Kernel.

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
from aikernel.llm.openai import OpenAIProvider
from aikernel.llm.anthropic import AnthropicProvider
from aikernel.llm.google import GoogleProvider
from aikernel.llm.groq import GroqProvider
from aikernel.llm.deepseek import DeepSeekProvider
from aikernel.llm.ollama import OllamaProvider
from aikernel.llm.openrouter import OpenRouterProvider
```
