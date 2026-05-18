from __future__ import annotations

from typing import Any, Dict

from krnlai.core.memory.episodic_memory import EpisodicMemory
from krnlai.core.memory.semantic_memory import SemanticMemory
from krnlai.core.memory.working_memory import WorkingMemory
from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class MemoryStep:
    def __init__(
        self,
        working_memory: WorkingMemory,
        episodic_memory: EpisodicMemory,
        semantic_memory: SemanticMemory,
    ) -> None:
        self.working_memory = working_memory
        self.episodic_memory = episodic_memory
        self.semantic_memory = semantic_memory

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        episodes = self.episodic_memory.recent(5)
        facts = self.semantic_memory.search(cmd.payload)
        wm_id = self.working_memory.store(cmd.payload)

        recall = [
            {
                "id": str(e.id),
                "type": e.episode_type,
                "preview": str(e.content)[:100] if e.content else "",
            }
            for e in episodes
        ]

        return {
            "working_memory_id": str(wm_id),
            "episodes_recalled": len(recall),
            "facts_recalled": len(facts),
            "recall": recall,
        }
