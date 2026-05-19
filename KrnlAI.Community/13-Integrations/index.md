# Integrations

Krnl-AI integrates with popular AI frameworks and tools.

## LangChain

```python
from krnlai.integrations.langchain import KrnlAITool

tool = KrnlAITool(safety_level="strict")
# Use with LangChain agents and chains
```

## CrewAI

```python
from krnlai.integrations.crewai import KrnlAIAgent

agent = KrnlAIAgent(safety_level="strict")
# Use in CrewAI crews and tasks
```

## AutoGen

```python
from krnlai.integrations.autogen import KrnlAIAssistantAgent

agent = KrnlAIAssistantAgent(name="krnlai", safety_level="strict")
# Use in AutoGen multi-agent conversations
```

## FastAPI

```python
from fastapi import FastAPI
from krnlai.integrations.fastapi import KrnlAIMiddleware

app = FastAPI()
app.add_middleware(KrnlAIMiddleware)
# All requests now pass through Krnl-AI safety checks
```

## Enterprise Mode

Connect the Python SDK to the C# enterprise backend for persistence and scale:

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent(
    mode="enterprise",
    endpoint="http://kernel-api:5001",
    api_key="sk-...",
)
```

### Auto-Detect

```python
# Automatically detects C# backend, falls back to standalone
agent = CognitiveAgent(mode="auto")
```

### Enterprise Components

| Component | Description |
|-----------|-------------|
| `EnterpriseClient` | HTTP/gRPC client for C# backend |
| `StreamingClient` | SSE streaming via async generator |
| `QdrantStore` | Vector store for semantic memory |
| `MySQLStore` | Persistent storage for cycles and audit trails |
