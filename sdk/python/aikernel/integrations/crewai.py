from __future__ import annotations

from typing import Any

from aikernel.core.cycle import CognitiveCycleRunner, CycleConfig


class AikernelAgent:
    def __init__(
        self,
        name: str = "AI Kernel Agent",
        role: str = "Cognitive Assistant",
        goal: str = "Process tasks with safety and cognitive awareness",
        backstory: str = "An AI agent powered by the AI Kernel cognitive cycle with 20 fundamental safety rules",
        safety_level: str = "strict",
        allow_delegation: bool = False,
        verbose: bool = False,
    ) -> None:
        self.name = name
        self.role = role
        self.goal = goal
        self.backstory = backstory
        self.allow_delegation = allow_delegation
        self.verbose = verbose

        config = CycleConfig(safety_level=safety_level)
        self._runner = CognitiveCycleRunner(config=config)

    async def execute_task(self, task: Any) -> str:
        if hasattr(task, "description"):
            prompt = task.description
        elif isinstance(task, str):
            prompt = task
        else:
            prompt = str(task)

        result = await self._runner.run(prompt)
        if self.verbose:
            print(f"[{self.name}] Risk: {result.risk_score:.2f}")
        return result.output

    def to_crewai_agent(self) -> Any:
        try:
            from crewai import Agent as CrewAIAgent
            return CrewAIAgent(
                role=self.role,
                goal=self.goal,
                backstory=self.backstory,
                allow_delegation=self.allow_delegation,
                verbose=self.verbose,
            )
        except ImportError:
            raise ImportError("crewai package required: pip install aikernel[crewai]")
