from __future__ import annotations

from typing import Any, Dict

from aikernel.core.models.cognitive import CognitiveState
from aikernel.core.models.envelope import CommandEnvelope


class ExecutionStep:
    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        output = f"Processed: {cmd.payload}"
        return {
            "output": output,
            "execution_status": "success",
            "execution_time_ms": 0.0,
        }
