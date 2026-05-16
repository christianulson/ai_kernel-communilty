from __future__ import annotations

from typing import Any, Dict, List

from aikernel.core.models.cognitive import CognitiveState
from aikernel.core.models.envelope import CommandEnvelope


class MetacognitionStep:
    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        risk = context.get("risk_score", 0.0)
        observations: List[str] = []

        if risk > 0.7:
            observations.append("high_risk_requires_caution")
        if risk > 0.4:
            observations.append("moderate_risk_monitor_closely")

        emotional_valence = context.get("emotional_valence", 0.0)
        if emotional_valence < -0.5:
            observations.append("negative_emotional_state")

        if len(cmd.payload) > 5000:
            observations.append("large_input_may_affect_performance")

        return {
            "observations": observations,
            "requires_intervention": len(observations) > 2,
            "metacognitive_confidence": max(0, 1.0 - risk),
        }
