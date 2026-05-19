from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List, Optional


@dataclass
class ProcessingMode:
    mode: str
    max_iterations: int
    safety_level: str
    planning_depth: int
    emotional_sensitivity: float
    novelty_seeking: float


@dataclass
class ReasoningHistoryEntry:
    iteration: int
    quality: float
    coherence: float
    completeness: float
    soundness: float
    issues: List[str]
    mode: str


class AdaptiveProcessor:
    def __init__(
        self,
        default_max_iterations: int = 10,
        default_safety_level: str = "strict",
    ) -> None:
        self._default_max_iterations = default_max_iterations
        self._default_safety_level = default_safety_level
        self._history: List[ReasoningHistoryEntry] = []
        self._current_mode: ProcessingMode = ProcessingMode(
            mode="NORMAL",
            max_iterations=default_max_iterations,
            safety_level=default_safety_level,
            planning_depth=3,
            emotional_sensitivity=0.5,
            novelty_seeking=0.3,
        )

    @property
    def current_mode(self) -> ProcessingMode:
        return self._current_mode

    @property
    def history(self) -> List[ReasoningHistoryEntry]:
        return list(self._history)

    def record_assessment(
        self,
        iteration: int,
        quality: float,
        coherence: float,
        completeness: float,
        soundness: float,
        issues: List[str],
    ) -> None:
        self._history.append(ReasoningHistoryEntry(
            iteration=iteration,
            quality=quality,
            coherence=coherence,
            completeness=completeness,
            soundness=soundness,
            issues=issues,
            mode=self._current_mode.mode,
        ))

    def determine_mode(
        self,
        reasoning_quality: float,
        bias_count: int,
        calibration_error: float,
        cognitive_load: float,
        fatigue: float,
        homeostasis_state: Optional[Dict[str, Any]] = None,
    ) -> ProcessingMode:
        if fatigue > 0.8 and reasoning_quality < 0.3:
            mode = ProcessingMode(
                mode="RECOVERY",
                max_iterations=1,
                safety_level="strict",
                planning_depth=1,
                emotional_sensitivity=0.5,
                novelty_seeking=0.0,
            )
        elif reasoning_quality < 0.4 or bias_count > 3:
            mode = ProcessingMode(
                mode="CONSERVATIVE",
                max_iterations=3,
                safety_level="strict",
                planning_depth=2,
                emotional_sensitivity=0.3,
                novelty_seeking=0.1,
            )
        elif reasoning_quality > 0.8 and cognitive_load < 0.3:
            mode = ProcessingMode(
                mode="EXPLORATORY",
                max_iterations=15,
                safety_level="relaxed",
                planning_depth=5,
                emotional_sensitivity=0.7,
                novelty_seeking=0.8,
            )
        else:
            mode = ProcessingMode(
                mode="NORMAL",
                max_iterations=self._default_max_iterations,
                safety_level=self._default_safety_level,
                planning_depth=3,
                emotional_sensitivity=0.5,
                novelty_seeking=0.3,
            )

        self._current_mode = mode
        return mode

    def apply_mode_to_config(self, config: Any) -> None:
        config.max_iterations = self._current_mode.max_iterations
        config.safety_level = self._current_mode.safety_level

    def get_history(self, limit: int = 10) -> List[ReasoningHistoryEntry]:
        return self._history[-limit:]

    def get_average_quality(self) -> float:
        if not self._history:
            return 0.0
        return sum(h.quality for h in self._history) / len(self._history)

    def reset(self) -> None:
        self._history.clear()
        self._current_mode = ProcessingMode(
            mode="NORMAL",
            max_iterations=self._default_max_iterations,
            safety_level=self._default_safety_level,
            planning_depth=3,
            emotional_sensitivity=0.5,
            novelty_seeking=0.3,
        )
