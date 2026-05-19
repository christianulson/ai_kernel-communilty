from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Optional, Tuple


@dataclass
class CalibratedConfidence:
    raw_confidence: float
    calibrated_confidence: float
    adjustment: float
    calibration_error: float
    reason: str


class ConfidenceCalibrator:
    MAX_HISTORY = 100

    def __init__(self) -> None:
        self._history: Dict[str, List[Tuple[float, bool]]] = {}

    def calibrate(
        self,
        raw_confidence: float,
        thought_type: str,
        reasoning_quality: float,
        bias_count: int,
        emotional_state: Optional[Dict[str, float]],
        complexity: float,
    ) -> CalibratedConfidence:
        adjustment = 0.0
        reasons: List[str] = []

        hist = self._history.get(thought_type, [])
        if hist:
            avg_error = sum(abs(c - (1.0 if r else 0.0)) for c, r in hist) / len(hist)
            adjustment -= avg_error * 0.3
            reasons.append(f"hist_error:{avg_error:.2f}")

        if reasoning_quality < 0.3:
            adjustment -= 0.2
            reasons.append("low_quality")
        elif reasoning_quality < 0.6:
            adjustment -= 0.1
            reasons.append("moderate_quality")

        adjustment -= bias_count * 0.05
        if bias_count > 0:
            reasons.append(f"biases:{bias_count}")

        if emotional_state:
            valence = emotional_state.get("valence", 0.0)
            if valence < -0.5:
                adjustment -= 0.1
                reasons.append("negative_valence")
            elif valence > 0.7:
                adjustment -= 0.05
                reasons.append("high_valence")

        adjustment -= complexity * 0.1
        if complexity > 0.5:
            reasons.append(f"complexity:{complexity:.1f}")

        calibrated = max(0.0, min(1.0, raw_confidence + adjustment))
        calibration_error = self._get_calibration_error(thought_type)
        reason = "; ".join(reasons) if reasons else "no_adjustment"

        return CalibratedConfidence(
            raw_confidence=raw_confidence,
            calibrated_confidence=calibrated,
            adjustment=adjustment,
            calibration_error=calibration_error,
            reason=reason,
        )

    def record_outcome(self, thought_type: str, confidence: float, was_correct: bool) -> None:
        if thought_type not in self._history:
            self._history[thought_type] = []
        self._history[thought_type].append((confidence, was_correct))
        self._history[thought_type] = self._history[thought_type][-self.MAX_HISTORY:]

    def get_calibration_error(self, thought_type: str) -> float:
        return self._get_calibration_error(thought_type)

    def _get_calibration_error(self, thought_type: str) -> float:
        hist = self._history.get(thought_type, [])
        if not hist:
            return 0.0
        return sum(abs(c - (1.0 if r else 0.0)) for c, r in hist) / len(hist)

    def get_calibration_curve(self) -> Dict[str, float]:
        return {tt: self._get_calibration_error(tt) for tt in self._history}

    def reset(self) -> None:
        self._history.clear()
