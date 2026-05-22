# SDK Guide

Krnl-AI provides SDKs for both Python and .NET to build agent applications programmatically.

## Python SDK

### Installation

```bash
pip install krnlai
```

### Quick Start

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")
response = await agent.run("analyze this dataset")
print(response.output)
```

### CognitiveAgent API

```python
agent = CognitiveAgent(
    mode="auto",             # "auto" | "standalone" | "enterprise"
    safety_level="strict",   # "strict" | "relaxed"
    endpoint="",             # C# backend URL (enterprise mode)
    api_key="",              # C# backend API key
    max_iterations=10,
    enable_emotions=True,
    enable_learning=True,
)
```

#### Methods

| Method | Description |
|--------|-------------|
| `run(command)` | Execute a single command |
| `run_command(envelope)` | Execute a `CommandEnvelope` |
| `stream(command)` | Stream cognitive cycle events |
| `close()` | Clean up resources |

### Available Modules

| Module | Description |
|--------|-------------|
| `krnlai.llm.openai` | OpenAI LLM provider |
| `krnlai.llm.anthropic` | Anthropic LLM provider |
| `krnlai.llm.ollama` | Ollama LLM provider |
| `krnlai.llm.google` | Google Gemini provider |
| `krnlai.llm.deepseek` | DeepSeek provider |
| `krnlai.llm.groq` | Groq provider |
| `krnlai.llm.openrouter` | OpenRouter provider |
| `krnlai.llm.mistral` | Mistral AI provider |
| `krnlai.llm.cohere` | Cohere provider |
| `krnlai.llm.together` | Together AI provider |
| `krnlai.core.safety` | Safety checker and rules |
| `krnlai.core.memory` | Episodic, semantic, working, procedural, autobiographical memory |
| `krnlai.core.emotion` | VAD emotional model + pain/reward system |
| `krnlai.core.policies` | Policy engine |
| `krnlai.core.risk` | Risk scoring |
| `krnlai.core.cognition` | Cognitive cycle, metacognition, inner speech |
| `krnlai.core.consciousness` | Operational consciousness, attention schema |
| `krnlai.investigation` | Causal investigation and root cause analysis |
| `krnlai.integrations` | LangChain, CrewAI, AutoGen, FastAPI |
| `krnlai.enterprise` | Enterprise client and stores |

### LLM Providers

```python
from krnlai.llm.ollama import OllamaProvider

provider = OllamaProvider(model="llama3.1", endpoint="http://localhost:11434/v1")
response = await provider.chat("Hello!")
```

## .NET SDK

### Installation

```bash
dotnet add package KrnlAISdk
```

### Quick Start

```csharp
using KrnlAI;

var client = new KrnlAIClient();
var response = await client.RunAsync("analyze this dataset");
Console.WriteLine(response.Narration);
```

### Models

| Model | Description |
|-------|-------------|
| `AgentRunRequest` | Request to run the agent |
| `MemorySearchRequest` | Request to search memory |
| `EpisodeModels` | Episode data models |
| `HealthModels` | Health check models |
| `MetricsModels` | Performance metrics models |
| `GoalModels` | Goal tracking models |

### Usage

```csharp
// Search memory
var memoryResult = await client.SearchMemoryAsync("project decision");

// Check health
var health = await client.CheckHealthAsync();

// Get metrics
var metrics = await client.GetMetricsAsync();
```
