from __future__ import annotations

from typing import Any, Dict

from aikernel.core.memory.episodic_memory import EpisodicMemory
from aikernel.core.models.cognitive import CognitiveState
from aikernel.core.models.envelope import CommandEnvelope


class OutcomeStep:
    def __init__(self, episodic_memory: EpisodicMemory) -> None:
        self.episodic_memory = episodic_memory

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        execution_output = context.get("output", "")
        episode_id = self.episodic_memory.remember(
            content={"input": cmd.payload, "output": execution_output},
            episode_type="cycle",
        )

        return {
            "episode_id": str(episode_id),
            "output": execution_output,
            "outcome_status": "recorded",
        }
