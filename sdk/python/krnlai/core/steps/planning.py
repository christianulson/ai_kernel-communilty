from __future__ import annotations

from typing import Any, Dict, List

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope

MAX_PLAN_STEPS = 10


class DynamicPlanningStep:
    MAX_PLAN_STEPS = MAX_PLAN_STEPS

    def __init__(self, max_plan_steps: int | None = None) -> None:
        self._max_plan_steps = max_plan_steps or self.MAX_PLAN_STEPS

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        complexity = context.get("complexity", 0.3)
        thought_type = context.get("thought_type", "analytical")
        cognitive_load = context.get("cognitive_load", 0.0)
        requires_decomposition = context.get("requires_decomposition", False)
        risk_score = context.get("risk_score", 0.0)
        observations = context.get("metacognitive_observations", [])

        if complexity < 0.3:
            plan = self._simple_plan(thought_type)
        elif complexity < 0.7:
            plan = self._moderate_plan(thought_type, cognitive_load)
        else:
            plan = self._complex_decomposition(cmd.payload, thought_type)

        plan = self._adapt_by_thought_type(plan, thought_type)
        plan = self._adapt_by_homeostasis(plan, context)
        plan = self._adapt_by_safety(plan, risk_score, requires_decomposition, observations)

        if len(plan) > self._max_plan_steps:
            plan = plan[:self._max_plan_steps]

        return {
            "plan": plan,
            "current_step_index": 0,
            "total_steps": len(plan),
            "decomposition_strategy": self._get_strategy_name(complexity),
        }

    def _simple_plan(self, thought_type: str) -> List[str]:
        return ["execute"]

    def _moderate_plan(self, thought_type: str, cognitive_load: float) -> List[str]:
        base = ["analyze", "execute", "verify"]
        if cognitive_load > 0.5:
            base.insert(1, "check_working_memory")
        return base

    def _complex_decomposition(self, payload: str, thought_type: str) -> List[str]:
        if thought_type == "analytical":
            return ["gather_data", "analyze", "conclude", "verify"]
        elif thought_type == "creative":
            return ["divergent_exploration", "convergent_selection", "develop", "critique", "refine"]
        elif thought_type == "critical":
            return ["identify_assumptions", "evaluate_evidence", "consider_alternatives", "formulate_conclusion"]
        elif thought_type == "procedural":
            return ["load_procedure", "execute_steps", "verify_outcome"]
        elif thought_type == "social":
            return ["consider_perspective", "formulate_response", "empathy_check", "respond"]
        else:
            return ["research", "decompose", "execute_step", "verify", "integrate"]

    def _adapt_by_thought_type(self, plan: List[str], thought_type: str) -> List[str]:
        adapted = list(plan)
        if thought_type == "analytical" and "analyze" not in adapted:
            adapted.insert(0, "analyze")
        elif thought_type == "creative" and "explore" not in adapted:
            adapted.append("explore")
        elif thought_type == "critical" and "evaluate_assumptions" not in adapted:
            adapted.insert(0, "evaluate_assumptions")
        elif thought_type == "procedural" and "verify_prerequisites" not in adapted:
            adapted.insert(0, "verify_prerequisites")
        return adapted

    def _adapt_by_homeostasis(self, plan: List[str], context: Dict[str, Any]) -> List[str]:
        fatigue = context.get("fatigue", 0.0)
        novelty_starvation = context.get("novelty_starvation", 0.0)

        adapted = list(plan)

        if fatigue > 0.7:
            adapted = adapted[:max(1, len(adapted) // 2)]
            adapted.append("check_energy")

        if novelty_starvation > 0.7 and len(adapted) < 5:
            adapted.insert(1, "explore_alternatives")

        return adapted

    def _adapt_by_safety(
        self,
        plan: List[str],
        risk_score: float,
        requires_decomposition: bool,
        observations: List[str],
    ) -> List[str]:
        adapted = list(plan)
        needs_safety = (
            "high_risk_requires_caution" in observations
            or risk_score > 0.7
            or requires_decomposition
        )
        if needs_safety and "validate_safety" not in adapted:
            adapted.append("validate_safety")
        return adapted

    def _get_strategy_name(self, complexity: float) -> str:
        if complexity < 0.3:
            return "simple"
        elif complexity < 0.7:
            return "moderate"
        return "complex_decomposition"
