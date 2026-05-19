from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from uuid import UUID, uuid4


@dataclass
class MomentSnapshot:
    moment_id: UUID = field(default_factory=uuid4)
    cycle_id: UUID = field(default_factory=uuid4)
    category: "MomentCategory" = None  # type: ignore[assignment]
    confidence: float = 0.0
    importance: "MomentImportance" = None  # type: ignore[assignment]
    narrative_role: "MomentNarrativeRole" = None  # type: ignore[assignment]
    cognitive_load: float = 0.0
    arousal: float = 0.0
    valence: float = 0.0
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    def __post_init__(self) -> None:
        if self.category is None:
            self.category = MomentCategory.ROUTINE
        if self.importance is None:
            self.importance = MomentImportance.ZERO
        if self.narrative_role is None:
            self.narrative_role = MomentNarrativeRole.NONE


class MomentCategory(str, Enum):
    ROUTINE = "routine"
    LEARNING = "learning"
    ANOMALY = "anomaly"
    CONFLICT = "conflict"


class MomentImportance(Enum):
    ZERO = 0
    LOW = 1
    MEDIUM = 2
    HIGH = 3
    CRITICAL = 4


class MomentNarrativeRole(str, Enum):
    NONE = "none"
    SETUP = "setup"
    TURNING_POINT = "turning_point"
    RESOLUTION = "resolution"
    CLIMAX = "climax"
