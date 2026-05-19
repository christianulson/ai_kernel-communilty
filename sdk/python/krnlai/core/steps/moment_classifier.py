from __future__ import annotations

from typing import Any, Dict, List

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.models.moment import (
    MomentCategory,
    MomentImportance,
    MomentNarrativeRole,
    MomentSnapshot,
)


class MomentClassifierStep:
    def __init__(self) -> None:
        self._moment_history: List[MomentSnapshot] = []

    @property
    def moment_history(self) -> List[MomentSnapshot]:
        return list(self._moment_history)

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        risk = context.get("risk_score", 0.0)
        has_error = len(state.errors) > 0
        novelty = context.get("novelty", self._estimate_novelty(cmd, context))
        emotional_valence = context.get("valence_delta", 0.0)

        has_new_info = any(k in context for k in ["recalled_facts", "features", "plan"])

        if has_error:
            category = MomentCategory.ANOMALY
        elif novelty > 0.7 or has_new_info:
            category = MomentCategory.LEARNING
        elif risk > 0.5:
            category = MomentCategory.CONFLICT
        else:
            category = MomentCategory.ROUTINE

        confidence = self._calculate_confidence(category, risk, novelty)
        importance = self._calculate_importance(risk, novelty, emotional_valence)
        role = self._determine_narrative_role(context)

        snapshot = MomentSnapshot(
            cycle_id=state.cycle_id,
            category=category,
            confidence=confidence,
            importance=importance,
            narrative_role=role,
            cognitive_load=min(1.0, risk + novelty * 0.5),
            arousal=abs(emotional_valence),
            valence=emotional_valence,
        )
        self._moment_history.append(snapshot)

        return {
            "moment_category": category,
            "moment_confidence": confidence,
            "moment_importance": importance,
            "moment_narrative_role": role,
            "moment_cognitive_load": snapshot.cognitive_load,
            "moment_arousal": snapshot.arousal,
            "moment_valence": snapshot.valence,
            "moment_id": snapshot.moment_id,
        }

    @staticmethod
    def _estimate_novelty(cmd: CommandEnvelope, context: Dict[str, Any]) -> float:
        payload = cmd.payload
        word_count = len(payload.split())
        if word_count > 100:
            return 0.6
        has_question = "?" in payload
        has_code = "```" in payload or "class " in payload or "def " in payload
        novelty = 0.0
        if has_question:
            novelty += 0.2
        if has_code:
            novelty += 0.3
        novelty += min(0.4, word_count / 200.0)
        return min(1.0, novelty)

    @staticmethod
    def _calculate_confidence(category: MomentCategory, risk: float, novelty: float) -> float:
        if category == MomentCategory.ROUTINE:
            return 0.9
        elif category == MomentCategory.ANOMALY:
            return max(0.5, 1.0 - risk * 0.5)
        elif category == MomentCategory.LEARNING:
            return min(0.9, 0.5 + novelty * 0.4)
        elif category == MomentCategory.CONFLICT:
            return max(0.4, 0.7 - risk * 0.3)
        return 0.5

    @staticmethod
    def _calculate_importance(risk: float, novelty: float, valence: float) -> MomentImportance:
        raw = risk * 0.4 + novelty * 0.4 + abs(valence) * 0.2
        if raw > 0.8:
            return MomentImportance.CRITICAL
        elif raw > 0.6:
            return MomentImportance.HIGH
        elif raw > 0.4:
            return MomentImportance.MEDIUM
        elif raw > 0.2:
            return MomentImportance.LOW
        return MomentImportance.ZERO

    @staticmethod
    def _determine_narrative_role(context: Dict[str, Any]) -> MomentNarrativeRole:
        risk = context.get("risk_score", 0.0)
        urgency = context.get("urgency", 0.0)
        if risk > 0.7 or urgency > 0.7:
            return MomentNarrativeRole.CLIMAX
        if risk > 0.4:
            return MomentNarrativeRole.TURNING_POINT
        if context.get("output"):
            return MomentNarrativeRole.RESOLUTION
        return MomentNarrativeRole.SETUP if len(context) > 5 else MomentNarrativeRole.NONE
