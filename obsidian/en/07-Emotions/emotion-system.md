# Emotion System

Krnl-AI includes an emotional model that influences decision-making based on the agent's internal state. This is optional and can be disabled.

## VAD Model

The emotional state is modeled using the **Valence-Arousal-Dominance (VAD)** dimensional model:

| Dimension | Range | Description |
|-----------|-------|-------------|
| **Valence** | -1.0 to 1.0 | Pleasure (positive ↔ negative) |
| **Arousal** | -1.0 to 1.0 | Intensity (calm ↔ excited) |
| **Dominance** | -1.0 to 1.0 | Control (submissive ↔ dominant) |

### State Properties

```python
from krnlai import VADState

state = VADState(valence=0.5, arousal=0.2, dominance=0.3)
state.is_positive   # True if valence > 0
state.is_negative   # True if valence < 0
state.is_calm       # True if |arousal| < 0.3
state.is_intense    # True if |arousal| > 0.7
```

## How Emotions Affect Behavior

| Emotional State | Effect |
|----------------|--------|
| **Negative valence** | Increases perceived risk (bias up to +0.2) |
| **High arousal** | Adds risk bias (+0.1 per unit) |
| **Positive valence** | Slightly decreases perceived risk |
| **Calm state** | Neutral, unbiased evaluation |

## Emotional Transitions

The emotional state changes based on events during the cognitive cycle:

| Event | Effect |
|-------|--------|
| High risk detected | Negative valence shift (-0.2), increased arousal |
| Successful execution | Positive valence shift (+0.05) |
| Natural decay | Gradual return to neutral (5% per step) |

```python
from krnlai.core.emotion.vad import VADModel

model = VADModel()
transition = model.update(
    delta_valence=-0.2,
    delta_arousal=0.3,
    trigger="high_risk_detected",
)
print(f"Previous: {transition.previous_state}")
print(f"Current: {transition.new_state}")
print(f"Delta: {transition.delta}")

# Emotional decay over time
model.decay(steps=3)
```

## Emotional Memory

All emotional transitions are recorded and can be queried:

```python
# Full timeline
model.history

# Search by trigger
emotional_memory = model.emotional_memory  # if available
emotional_memory.search_by_trigger("error")
```

## Pain/Reward Model

In addition to the VAD model, a pain/reward system provides reinforcement learning signals:

```python
from krnlai.core.emotion.pain_reward import PainRewardModel

pain_reward = PainRewardModel()
pain_reward.apply_many([
    {"type": "reward", "value": 0.5, "reason": "task_completed"},
    {"type": "pain", "value": -0.1, "reason": "high_risk"},
])
```

## Configuration

```python
agent = CognitiveAgent(enable_emotions=True)  # default
```

When disabled, emotional state is always neutral and no emotional memory is recorded.
