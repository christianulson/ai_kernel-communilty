from __future__ import annotations

from typing import Any, Dict

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class SensorStep:
    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "input": cmd.payload,
            "context": cmd.context,
            "sensed_at": state.started_at.isoformat(),
        }
