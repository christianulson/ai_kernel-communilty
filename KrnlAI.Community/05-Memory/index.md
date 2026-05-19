# Memory System

Krnl-AI implements multiple memory types, each serving a distinct cognitive function. All memory is stored locally via SQLite in community mode.

## Memory Types

| Type | Purpose | Persistence |
|------|---------|-------------|
| **Working Memory** | Immediate context for the current cycle | Volatile (in-memory) |
| **Episodic Memory** | History of past execution cycles | SQLite |
| **Semantic Memory** | Factual knowledge and relationships | SQLite (vectors) |
| **Emotional Memory** | History of emotional state transitions | SQLite |

## Working Memory

Stores the current input and intermediate processing state during a cognitive cycle.

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
agent.working_memory.store("current context")
context = agent.working_memory.recall()
```

## Episodic Memory

Records each cognitive cycle as an episode with input, output, timestamp, and metadata.

```python
# Episodic memory is automatically populated after each cycle
# You can query recent episodes:
agent.episodic_memory.recent(5)

# Search by episode type:
agent.episodic_memory.search("cycle")
```

## Semantic Memory

Stores factual knowledge that can be retrieved via semantic search.

```python
# Store a fact
agent.semantic_memory.store_fact(
    subject="project",
    predicate="uses",
    object_val="SQLite",
    confidence=0.9,
)

# Search for relevant facts
results = agent.semantic_memory.search("storage backend")
```

## Emotional Memory

Tracks emotional state transitions over time.

```python
# View emotional timeline
agent.emotional_memory.timeline()

# Search emotional history by trigger
agent.emotional_memory.search_by_trigger("error")

# Count recorded states
agent.emotional_memory.count
```

## CLI Memory Commands

```bash
krnlai memory search "my query"
krnlai memory snapshot
krnlai memory metrics
```

## Sidecar Memory API

```bash
# Search memory via HTTP
curl -X POST http://localhost:5001/memory/search \
  -H "Content-Type: application/json" \
  -d '{"query": "project decision"}'

# Get memory metrics
curl http://localhost:5001/memory/metrics
```
