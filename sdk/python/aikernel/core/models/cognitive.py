from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class CycleStep(str, Enum):
    SENSOR = "sensor"
    ATTENTION = "attention"
    MEMORY = "memory"
    EVALUATION = "evaluation"
    METACOGNITION = "metacognition"
    PLANNING = "planning"
    GOVERNANCE = "governance"
    EXECUTION = "execution"
    OUTCOME = "outcome"
    LEARNING = "learning"


class CyclePhase(str, Enum):
    PERCEPTION = "perception"
    DELIBERATION = "deliberation"
    ACTION = "action"
    REFLECTION = "reflection"


class CognitiveState(BaseModel):
    cycle_id: UUID = Field(default_factory=uuid4)
    current_step: CycleStep = CycleStep.SENSOR
    phase: CyclePhase = CyclePhase.PERCEPTION
    iteration: int = 0
    command: Optional[str] = None
    context: Dict[str, Any] = Field(default_factory=dict)
    emotional_state: Optional[Dict[str, float]] = None
    risk_score: float = 0.0
    safety_verdict: Optional[SafetyVerdict] = None
    errors: List[str] = Field(default_factory=list)
    started_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    completed_at: Optional[datetime] = None

    @property
    def duration_ms(self) -> float:
        end = self.completed_at or datetime.now(timezone.utc)
        return (end - self.started_at).total_seconds() * 1000


class CycleEvent(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    cycle_id: UUID
    step: CycleStep
    status: str = "running"
    data: Dict[str, Any] = Field(default_factory=dict)
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    duration_ms: float = 0.0


from aikernel.core.models.safety import SafetyVerdict
