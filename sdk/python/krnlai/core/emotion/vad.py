from __future__ import annotations

import math
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4


@dataclass
class VADState:
    valence: float = 0.0
    arousal: float = 0.0
    dominance: float = 0.0

    def clamp(self) -> VADState:
        return VADState(
            valence=max(-1.0, min(1.0, self.valence)),
            arousal=max(-1.0, min(1.0, self.arousal)),
            dominance=max(-1.0, min(1.0, self.dominance)),
        )

    @property
    def is_positive(self) -> bool:
        return self.valence > 0.0

    @property
    def is_negative(self) -> bool:
        return self.valence < 0.0

    @property
    def is_calm(self) -> bool:
        return abs(self.arousal) < 0.3

    @property
    def is_intense(self) -> bool:
        return abs(self.arousal) > 0.7

    def distance_to(self, other: VADState) -> float:
        return math.sqrt(
            (self.valence - other.valence) ** 2
            + (self.arousal - other.arousal) ** 2
            + (self.dominance - other.dominance) ** 2
        )

    def to_dict(self) -> Dict[str, float]:
        return {"valence": self.valence, "arousal": self.arousal, "dominance": self.dominance}


@dataclass
class EmotionalTransition:
    previous_state: VADState
    new_state: VADState
    id: UUID = field(default_factory=uuid4)
    trigger: str = ""
    intensity: float = 0.0
    metadata: Dict[str, Any] = field(default_factory=dict)

    @property
    def delta(self) -> Dict[str, float]:
        return {
            "valence": self.new_state.valence - self.previous_state.valence,
            "arousal": self.new_state.arousal - self.previous_state.arousal,
            "dominance": self.new_state.dominance - self.previous_state.dominance,
        }


class VADModel:
    def __init__(self, initial_state: Optional[VADState] = None) -> None:
        self._current = initial_state or VADState()
        self._history: List[EmotionalTransition] = []
        self._decay_rate: float = 0.05

    @property
    def current(self) -> VADState:
        return self._current

    def update(
        self,
        delta_valence: float = 0.0,
        delta_arousal: float = 0.0,
        delta_dominance: float = 0.0,
        trigger: str = "",
        intensity: float = 0.0,
    ) -> EmotionalTransition:
        previous = self._current
        new_state = VADState(
            valence=previous.valence + delta_valence,
            arousal=previous.arousal + delta_arousal,
            dominance=previous.dominance + delta_dominance,
        ).clamp()

        transition = EmotionalTransition(
            previous_state=previous,
            new_state=new_state,
            trigger=trigger,
            intensity=intensity,
        )
        self._history.append(transition)
        self._current = new_state
        return transition

    def decay(self, steps: int = 1) -> None:
        for _ in range(steps):
            self._current = VADState(
                valence=self._current.valence * (1 - self._decay_rate),
                arousal=self._current.arousal * (1 - self._decay_rate),
                dominance=self._current.dominance * (1 - self._decay_rate),
            ).clamp()

    def modulate_risk(self, risk_score: float) -> float:
        negative_bias = max(0, -self._current.valence) * 0.2
        arousal_bias = abs(self._current.arousal) * 0.1
        return min(1.0, risk_score + negative_bias + arousal_bias)

    @property
    def history(self) -> List[EmotionalTransition]:
        return list(self._history)

    def reset(self, state: Optional[VADState] = None) -> None:
        self._current = state or VADState()
        self._history.clear()
