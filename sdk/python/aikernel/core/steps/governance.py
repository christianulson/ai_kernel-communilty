from __future__ import annotations

from typing import Any, Dict

from aikernel.core.models.cognitive import CognitiveState
from aikernel.core.models.envelope import CommandEnvelope
from aikernel.core.policies.engine import PolicyEngine


class GovernanceStep:
    def __init__(self, policy_engine: PolicyEngine) -> None:
        self.policies = policy_engine

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        policy_context = {
            "action": "kernel.handle",
            "payload": cmd.payload,
            "risk_score": context.get("risk_score", 0.0),
            "action_level": 1,
            "role": "user",
        }
        result = self.policies.execute(policy_context)
        return {
            "policy_allowed": result.get("allowed", True),
            "policy_actions": result.get("actions", []),
            "governance_result": "passed" if result.get("allowed", True) else "blocked",
        }
