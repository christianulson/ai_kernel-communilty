# Enterprise Mode

Connect the Python SDK to the C# AI Kernel backend for persistence, multi-tenancy, and scale.

## Setup

```python
from aikernel import CognitiveAgent

agent = CognitiveAgent(
    mode="enterprise",
    endpoint="http://kernel-api:5001",
    api_key="sk-...",
)
```

## Auto-Detect

```python
# Automatically detects C# backend. Falls back to standalone.
agent = CognitiveAgent(mode="auto")
```

## Components

- **EnterpriseClient**: HTTP/gRPC client for C# backend
- **StreamingClient**: SSE streaming via async generator
- **QdrantStore**: Vector store for semantic memory
- **MySQLStore**: Persistent storage for cycles and audit trails
