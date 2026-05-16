from __future__ import annotations

from typing import Any, Dict, List

from aikernel.core.models.cognitive import CognitiveState
from aikernel.core.models.envelope import CommandEnvelope


class PlanningStep:
    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        observations = context.get("metacognitive_observations", [])
        plan: List[str] = ["analyze"]

        if "high_risk_requires_caution" in observations:
            plan.append("validate_safety")
        else:
            plan.append("execute")

        plan.append("verify")

        return {
            "plan": plan,
            "current_step_index": 0,
            "total_steps": len(plan),
            "requires_approval": "validate_safety" in plan,
        }
