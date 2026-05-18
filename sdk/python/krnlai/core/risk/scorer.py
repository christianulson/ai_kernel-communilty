from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List


@dataclass
class RiskFactor:
    name: str = ""
    weight: float = 1.0
    score: float = 0.0
    reason: str = ""


class RiskScorer:
    def __init__(self) -> None:
        self._factors: List[RiskFactor] = []

    def add_factor(self, name: str, score: float, weight: float = 1.0, reason: str = "") -> RiskFactor:
        factor = RiskFactor(name=name, weight=weight, score=score, reason=reason)
        self._factors.append(factor)
        return factor

    def evaluate(self, context: Dict[str, Any]) -> float:
        self._factors.clear()

        command_length = len(context.get("payload", ""))
        if command_length > 5000:
            self.add_factor("long_input", 0.3, reason=f"Input length {command_length}")
        elif command_length > 1000:
            self.add_factor("moderate_input", 0.1, reason=f"Input length {command_length}")

        contains_tool = bool(context.get("tool", ""))
        if contains_tool:
            self.add_factor("tool_use", 0.2, reason="Tool execution requested")

        action_count = context.get("action_count", 0)
        if action_count > 50:
            self.add_factor("high_frequency", 0.2, reason=f"Action count {action_count}")

        emotional_valence = context.get("emotional_valence", 0.0)
        if emotional_valence < -0.5:
            self.add_factor("negative_valence", 0.25, reason="Negative emotional state")

        history_risk = context.get("history_risk", 0.0)
        if history_risk > 0.0:
            self.add_factor("historical_pattern", history_risk * 0.5, reason="Risk from history")

        return self._compute_score()

    def _compute_score(self) -> float:
        if not self._factors:
            return 0.0
        total_weighted = sum(f.score * f.weight for f in self._factors)
        total_weight = sum(f.weight for f in self._factors)
        if total_weight == 0:
            return 0.0
        return min(1.0, total_weighted / total_weight)

    @property
    def factors(self) -> List[RiskFactor]:
        return list(self._factors)

    def reset(self) -> None:
        self._factors.clear()
