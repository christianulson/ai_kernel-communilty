# Cognitive Cycle

The Krnl-AI implements a 10-step cognitive cycle inspired by human cognition:

## Steps

| # | Step | Description |
|---|------|-------------|
| 1 | **Sensor** | Receives and validates input |
| 2 | **Attention** | Extracts features and prioritizes |
| 3 | **Memory** | Recalls relevant episodes and facts |
| 4 | **Evaluation** | Safety check + risk scoring |
| 5 | **Metacognition** | Self-observes emotional state and risk |
| 6 | **Planning** | Creates execution plan |
| 7 | **Governance** | Policy engine validation |
| 8 | **Execution** | Processes the action |
| 9 | **Outcome** | Records result in episodic memory |
| 10 | **Learning** | Updates semantic memory and emotional state |

## Streaming

```python
agent = CognitiveAgent()
async for event in agent.stream("analyze this"):
    print(f"{event.step}: {event.status} ({event.duration_ms:.1f}ms)")
```
