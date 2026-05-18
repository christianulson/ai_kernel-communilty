from __future__ import annotations

from typing import Any, Dict

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.risk.scorer import RiskScorer
from krnlai.core.safety.rules import SafetyChecker


class EvaluationStep:
    def __init__(self, safety_checker: SafetyChecker, risk_scorer: RiskScorer) -> None:
        self.safety = safety_checker
        self.risk_scorer = risk_scorer

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        safety_context = {
            "action": "kernel.handle",
            "payload": cmd.payload,
            "context_id": str(state.cycle_id),
            "action_count": context.get("action_count", 0),
        }
        verdict = self.safety.evaluate_all(safety_context)
        risk_score = self.risk_scorer.evaluate(cmd.context)

        return {
            "safety_verdict": verdict,
            "risk_score": risk_score,
            "risk_level": verdict.risk_level.value if hasattr(verdict, "risk_level") else "unknown",
            "allowed": verdict.allowed,
        }
