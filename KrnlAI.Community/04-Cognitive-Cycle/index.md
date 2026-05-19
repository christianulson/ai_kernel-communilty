# Cognitive Cycle

The Krnl-AI cognitive runtime implements a 10-step cognitive processing cycle inspired by human cognition. Each step processes input through a different cognitive function.

## The 10 Steps

| # | Step | Description |
|---|------|-------------|
| 1 | **Sensor** | Receives and validates raw input |
| 2 | **Attention** | Extracts features and prioritizes information |
| 3 | **Memory** | Recalls relevant episodes and semantic facts |
| 4 | **Evaluation** | Safety check + risk scoring |
| 5 | **Metacognition** | Self-observes emotional state and risk level |
| 6 | **Planning** | Creates an execution plan |
| 7 | **Governance** | Policy engine validation |
| 8 | **Execution** | Processes the action |
| 9 | **Outcome** | Records result in episodic memory |
| 10 | **Learning** | Updates semantic memory and emotional state |

## Cycle Phases

The cycle progresses through four high-level phases:

```
PERCEPTION → DELIBERATION → ACTION → REFLECTION
   (steps 1-3)   (steps 4-7)   (step 8)   (steps 9-10)
```

## Iterations

The cognitive cycle runs in iterations (default max: 10). Each iteration processes all 10 steps. If errors occur in strict safety mode, the cycle stops immediately.

## Streaming

You can observe the cycle in real-time:

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
async for event in agent.stream("analyze this data"):
    print(f"{event.step}: {event.status} ({event.duration_ms:.1f}ms)")
```

## Step Details

### 1. Sensor

Validates and stores raw input in the step context. Checks for empty or invalid payloads.

### 2. Attention

Extracts features from the input:
- Length and word count
- Whether the input contains a question
- Whether the input starts with a command prefix (`/`, `!`, `run`)

### 3. Memory

Recalls relevant context:
- Last 3 episodic memories
- Semantic facts matching the input
- Stores input in working memory

### 4. Evaluation

Runs the full safety pipeline against the input:
- Allowlist check (only `kernel.handle` actions permitted)
- Risk scoring
- Input validation
- Emotional impact assessment

### 5. Metacognition

Self-observes the current state:
- High risk detected → caution flag
- Negative emotional state → awareness
- High arousal → inhibition bias

### 6. Planning

Creates a simple 3-step plan: `analyze → execute → verify`.

### 7. Governance

Applies the policy engine to validate the planned action against learned policies.

### 8. Execution

Processes the input and produces output.

### 9. Outcome

Records the full input/output pair as an episodic memory entry.

### 10. Learning

Updates semantic memory with a new fact about the processed input. Decays emotional state naturally.

## Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `max_iterations` | 10 | Maximum number of cycles |
| `step_timeout_ms` | 5000 | Per-step timeout |
| `cycle_timeout_ms` | 30000 | Total cycle timeout |
| `safety_level` | `strict` | `strict` or `relaxed` |
| `enable_emotions` | `true` | Enable emotional model |
| `enable_learning` | `true` | Enable policy learning |
