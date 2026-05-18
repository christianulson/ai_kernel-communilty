from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Dict
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class VADState(BaseModel):
    valence: float = 0.0
    arousal: float = 0.0
    dominance: float = 0.0

    def clamped(self) -> VADState:
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
        return self.arousal < 0.3

    @property
    def is_intense(self) -> bool:
        return self.arousal > 0.7


class EmotionalEvent(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    cycle_id: UUID
    previous_state: VADState
    new_state: VADState
    trigger: str = ""
    intensity: float = 0.0
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    metadata: Dict[str, Any] = Field(default_factory=dict)

    @property
    def delta(self) -> Dict[str, float]:
        return {
            "valence": self.new_state.valence - self.previous_state.valence,
            "arousal": self.new_state.arousal - self.previous_state.arousal,
            "dominance": self.new_state.dominance - self.previous_state.dominance,
        }
