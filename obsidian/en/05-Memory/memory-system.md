# Memory System

Krnl-AI implements multiple memory types modeled after human cognition. All memory is stored locally via SQLite in community mode.

## Memory Types

| Type | Purpose | Persistence |
|------|---------|-------------|
| **Working Memory** | Immediate context for the current cycle | Volatile (in-memory) |
| **Episodic Memory** | History of past execution cycles | SQLite |
| **Semantic Memory** | Factual knowledge and relationships | SQLite (vectors) |
| **Procedural Memory** | Learned procedures and skills | SQLite |
| **Emotional Memory** | History of emotional state transitions | SQLite |
| **Autobiographical Memory** | Narrative of the agent's own history | SQLite |
| **Prospective Memory** | Future intentions and pending goals | SQLite |

## Working Memory

Stores the current input and intermediate processing state during a cognitive cycle. Includes attention-based filtering and time-to-live eviction.

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
agent.working_memory.store("current context")
context = agent.working_memory.recall()
```

## Episodic Memory

Records each cognitive cycle as an episode with input, output, timestamp, and metadata. Supports temporal search and associative recall.

```python
# Episodic memory is automatically populated after each cycle
# You can query recent episodes:
agent.episodic_memory.recent(5)

# Search by episode type:
agent.episodic_memory.search("cycle")

# Temporal search within a time range:
agent.episodic_memory.search_temporal("cycle", since="2026-01-01")
```

## Semantic Memory

Stores factual knowledge as subject-predicate-object triples with confidence scores. Supports vector-based semantic search.

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

## Procedural Memory

Stores learned procedures — sequences of actions that have been successful in the past. Procedures are automatically extracted from successful plan executions.

```python
# List learned procedures
agent.procedural_memory.list()

# Search procedures by trigger pattern
agent.procedural_memory.search("data analysis")

# Apply a procedure
agent.procedural_memory.apply("analyze_dataset")
```

## Autobiographical Memory

Maintains a narrative of the agent's own history, linking related episodes into coherent stories. Used for self-reflection and identity continuity.

```python
# Get narrative summary
agent.autobiographical_memory.narrative()

# Search by life period
agent.autobiographical_memory.search_by_period("last_week")
```

## Prospective Memory

Stores future intentions and pending goals. The agent periodically checks prospective memory to act on delayed intentions.

```python
# Set a future intention
agent.prospective_memory.remember(
    intention="send weekly report",
    trigger="friday 5pm",
)

# Check pending intentions
agent.prospective_memory.pending()
```

## Emotional Memory

Tracks emotional state transitions over time with VAD (Valence-Arousal-Dominance) snapshots and trigger annotations.

```python
# View emotional timeline
agent.emotional_memory.timeline()

# Search emotional history by trigger
agent.emotional_memory.search_by_trigger("error")

# Count recorded states
agent.emotional_memory.count
```

## Memory Ranking

When recalling information, the kernel ranks memory items using multiple signals:

- **Recency** — How recently the memory was accessed
- **Relevance** — Semantic similarity to current context
- **Emotional salience** — Emotional impact of the memory
- **Temporal pattern** — Seasonal or cyclic patterns
- **Analogical match** — Similarity to past situations

## CLI Memory Commands

```bash
# Search memory
krnlai memory search "my query"

# Take a memory snapshot
krnlai memory snapshot

# View memory metrics
krnlai memory metrics

# List procedures
krnlai skill list
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
