from __future__ import annotations

from typing import Any, Dict, List, Optional

from krnlai.core.cycle import CognitiveCycleRunner, CycleConfig


class KrnlAIAssistantAgent:
    def __init__(
        self,
        name: str = "KrnlAIAgent",
        system_message: Optional[str] = None,
        safety_level: str = "strict",
        human_input_mode: str = "NEVER",
        max_consecutive_auto_reply: int = 10,
    ) -> None:
        self.name = name
        self._system_message = system_message or (
            "You are an AI agent powered by the Krnl-AI cognitive cycle. "
            "You have a built-in safety system with 20 fundamental rules."
        )
        self.human_input_mode = human_input_mode
        self.max_consecutive_auto_reply = max_consecutive_auto_reply

        config = CycleConfig(safety_level=safety_level)
        self._runner = CognitiveCycleRunner(config=config)

    async def generate_reply(self, messages: List[Dict[str, Any]]) -> str:
        last_message = messages[-1].get("content", "") if messages else ""
        result = await self._runner.run(last_message)
        return result.output

    def to_autogen_agent(self) -> Any:
        try:
            from autogen import AssistantAgent
            return AssistantAgent(
                name=self.name,
                system_message=self._system_message,
                human_input_mode=self.human_input_mode,
                max_consecutive_auto_reply=self.max_consecutive_auto_reply,
            )
        except ImportError:
            raise ImportError("pyautogen package required: pip install krnlai[autogen]")
