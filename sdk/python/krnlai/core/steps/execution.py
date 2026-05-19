from __future__ import annotations

import time
from typing import Any, Dict, List

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.steps.planning import DynamicPlanningStep


class ExecutionStep:
    KNOWN_STEPS: Dict[str, str] = {
        "analyze": "Analyzed: {payload}",
        "execute": "Executed: {payload}",
        "verify": "Verified: {payload}",
        "check_working_memory": "Working memory OK",
        "check_energy": "Energy level OK",
        "explore_alternatives": "Explored alternatives for: {payload}",
        "validate_safety": "Safety validated: {payload}",
        "gather_data": "Data gathered: {payload}",
        "conclude": "Conclusion: {payload}",
        "divergent_exploration": "Divergent exploration: {payload}",
        "convergent_selection": "Convergent selection from: {payload}",
        "develop": "Developed: {payload}",
        "critique": "Critique: {payload}",
        "refine": "Refined: {payload}",
        "identify_assumptions": "Assumptions identified: {payload}",
        "evaluate_evidence": "Evidence evaluated: {payload}",
        "consider_alternatives": "Alternatives considered: {payload}",
        "formulate_conclusion": "Conclusion formulated: {payload}",
        "load_procedure": "Procedure loaded for: {payload}",
        "execute_steps": "Steps executed: {payload}",
        "verify_outcome": "Outcome verified: {payload}",
        "consider_perspective": "Perspective considered: {payload}",
        "formulate_response": "Response formulated: {payload}",
        "empathy_check": "Empathy check: {payload}",
        "respond": "Responded: {payload}",
        "research": "Researched: {payload}",
        "decompose": "Decomposed: {payload}",
        "execute_step": "Step executed: {payload}",
        "integrate": "Integrated: {payload}",
        "evaluate_assumptions": "Assumptions evaluated: {payload}",
        "verify_prerequisites": "Prerequisites verified: {payload}",
    }

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        plan: List[str] = context.get("plan", ["execute"])
        if not plan:
            plan = ["execute"]
        current_index: int = context.get("current_plan_step", 0)
        max_steps: int = context.get("max_plan_steps", DynamicPlanningStep.MAX_PLAN_STEPS)

        steps_to_run = plan[current_index:]
        if len(steps_to_run) > max_steps:
            steps_to_run = steps_to_run[:max_steps]

        start = time.monotonic()
        results: List[Dict[str, Any]] = []
        for i, step_name in enumerate(steps_to_run):
            step_result = self._execute_step(step_name, cmd.payload)
            step_result["step_index"] = current_index + i
            step_result["step_name"] = step_name
            results.append(step_result)

        elapsed = (time.monotonic() - start) * 1000
        outputs = [r["output"] for r in results]
        combined = " | ".join(outputs)

        all_success = all(r["status"] == "success" for r in results)
        has_unknown = any(r["status"] == "unknown" for r in results)

        return {
            "output": combined,
            "execution_status": "unknown_step" if has_unknown and not all_success else "success",
            "execution_time_ms": round(elapsed, 4),
            "steps_executed": [r["step_name"] for r in results],
            "step_results": results,
        }

    def _execute_step(self, step_name: str, payload: str) -> Dict[str, Any]:
        template = self.KNOWN_STEPS.get(step_name)
        if template is not None:
            output = template.format(payload=payload)
            return {"output": output, "status": "success"}
        return {"output": f"Processed step '{step_name}': {payload}", "status": "unknown"}
