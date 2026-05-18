from __future__ import annotations

from typing import Any, Dict

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class AttentionStep:
    def extract_features(self, text: str) -> Dict[str, Any]:
        return {
            "length": len(text),
            "has_question": "?" in text,
            "has_command": text.startswith(("/", "!", "run")),
            "word_count": len(text.split()),
            "has_code": "```" in text or "def " in text or "class " in text,
        }

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        features = self.extract_features(cmd.payload)
        return {"features": features, "attention_focus": "text"}
