# Cognitive Cycle

The Krnl-AI cognitive runtime implements a 10-step cognitive processing cycle inspired by human cognition. Each step processes input through a different cognitive function.

## The 10 Steps

| # | Step | Description |
|---|------|-------------|
| 1 | **Sensor** | Receives and validates raw input |
| 2 | **Attention** | Extracts features and prioritizes information |
| 3 | **Memory** | Recalls relevant episodes, semantic facts, and procedural knowledge |
| 4 | **Evaluation** | Safety check + risk scoring + emotional impact assessment |
| 5 | **Metacognition** | Self-observes emotional state, risk level, and cognitive biases |
| 6 | **Planning** | Creates an execution plan with sub-steps |
| 7 | **Governance** | Policy engine validation against learned policies |
| 8 | **Execution** | Processes the action through allowed toolset |
| 9 | **Outcome** | Records result in episodic memory |
| 10 | **Learning** | Updates semantic memory, policies, and emotional state |

## Cycle Phases

The cycle progresses through four high-level phases:

```
PERCEPTION → DELIBERATION → ACTION → REFLECTION
   (steps 1-3)   (steps 4-7)   (step 8)   (steps 9-10)
```

## Adaptive Loop

In addition to the standard cycle, the kernel supports an **adaptive loop** that modulates behavior based on task complexity and past outcomes. The adaptive loop can adjust:

- **Cycle depth** — Shallow (fast) vs deep (thorough) processing
- **Planning effort** — Heuristic decomposition for simple tasks vs full planning for complex ones
- **Memory recall breadth** — Narrow (recent only) vs broad (temporal + analogical) search
- **Metacognitive oversight** — Minimal for routine tasks, maximum for high-risk situations

## Coding Cognitive Cycle

For code processing tasks, the kernel runs a specialized **11-step coding cognitive cycle**:

| # | Step | Description |
|---|------|-------------|
| 1 | **CodeUnderstanding** | Parse and understand the code context |
| 2 | **IntentResolution** | Determine the user's intent |
| 3 | **ImpactAnalysis** | Analyze impact of changes |
| 4 | **SafetyCheck** | Validate code safety |
| 5 | **DiffGeneration** | Generate code diff |
| 6 | **AutoReview** | Self-review the generated diff |
| 7 | **RiskScoring** | Score the risk of the change |
| 8 | **Approval** | Request/auto-approve |
| 9 | **Apply** | Apply the diff |
| 10 | **Verify** | Verify the change |
| 11 | **Learning** | Learn from the outcome |

## Inner Speech and Higher-Order Thought

During the cycle, the kernel generates **inner speech** (step-by-step reasoning narration) and **higher-order thoughts** (self-awareness of the current cognitive state). These are available for inspection:

```python
async for event in agent.stream("analyze this data"):
    print(f"[{event.step}] {event.narration}")
    if event.inner_speech:
        print(f"  Inner: {event.inner_speech}")
    if event.higher_order_thought:
        print(f"  Meta: {event.higher_order_thought}")
```

## Iterations

The cognitive cycle runs in iterations (default max: 10). Each iteration processes all steps. If errors occur in strict safety mode, the cycle stops immediately.

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
- Recent episodic memories
- Semantic facts matching the input
- Procedural knowledge (learned procedures)
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
- Cognitive bias detection

### 6. Planning

Creates an execution plan. For simple tasks: `analyze → execute → verify`. For complex tasks, uses hierarchical decomposition with sub-goals and parallel steps.

### 7. Governance

Applies the policy engine to validate the planned action against learned policies.

### 8. Execution

Processes the input and produces output.

### 9. Outcome

Records the full input/output pair as an episodic memory entry.

### 10. Learning

Updates semantic memory with new facts. Updates policies based on outcome success. Decays emotional state naturally.

## Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `max_iterations` | 10 | Maximum number of cycles |
| `step_timeout_ms` | 5000 | Per-step timeout |
| `cycle_timeout_ms` | 30000 | Total cycle timeout |
| `safety_level` | `strict` | `strict` or `relaxed` |
| `enable_emotions` | `true` | Enable emotional model |
| `enable_learning` | `true` | Enable policy learning |
| `enable_inner_speech` | `true` | Enable inner speech narration |
