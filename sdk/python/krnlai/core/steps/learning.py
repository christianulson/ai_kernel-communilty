from __future__ import annotations

from typing import Any, Dict, Optional

from krnlai.core.cognition.confidence import ConfidenceCalibrator
from krnlai.core.emotion.pain_reward import PainRewardModel
from krnlai.core.emotion.vad import VADModel
from krnlai.core.memory.semantic_memory import SemanticMemory
from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class LearningStep:
    def __init__(
        self,
        semantic_memory: SemanticMemory,
        vad_model: VADModel,
        pain_reward: PainRewardModel,
        confidence_calibrator: Optional[ConfidenceCalibrator] = None,
        enabled: bool = True,
    ) -> None:
        self.semantic_memory = semantic_memory
        self.vad = vad_model
        self.pain_reward = pain_reward
        self.confidence_calibrator = confidence_calibrator
        self.enabled = enabled

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        if not self.enabled:
            return {"learning_status": "disabled"}

        self.semantic_memory.store_fact(
            subject="agent",
            predicate="processed",
            object_val=cmd.payload[:100],
            confidence=0.8,
        )

        if self.confidence_calibrator:
            thought_type = context.get("thought_type", "analytical")
            calibrated = context.get("calibrated_confidence", 0.5)
            was_error = bool(context.get("errors", []))
            was_blocked = context.get("safety_verdict") and not context["safety_verdict"].allowed
            was_correct = not was_error and not was_blocked
            self.confidence_calibrator.record_outcome(thought_type, calibrated, was_correct)

        self.vad.decay(steps=1)

        return {
            "learning_status": "completed",
            "facts_stored": 1,
            "emotional_decay": True,
        }
