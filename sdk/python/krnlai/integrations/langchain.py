from __future__ import annotations

from typing import Any, Dict

from krnlai.core.cycle import CognitiveCycleRunner


class KrnlAITool:
    def __init__(self, runner: CognitiveCycleRunner, name: str = "krnlai") -> None:
        self._runner = runner
        self.name = name
        self.description = "Krnl-AI cognitive agent with safety system"

    async def _arun(self, query: str) -> str:
        result = await self._runner.run(query)
        return result.output

    def run(self, query: str) -> str:
        import asyncio
        try:
            loop = asyncio.get_running_loop()
        except RuntimeError:
            return asyncio.run(self._arun(query))
        return loop.run_until_complete(self._arun(query))

    def to_langchain_tool(self) -> Any:
        try:
            from langchain.tools import BaseTool

            class _KrnlAILangChainTool(BaseTool):
                name: str = self.name
                description: str = self.description
                _runner: CognitiveCycleRunner = self._runner

                def _run(self, query: str) -> str:
                    import asyncio
                    result = asyncio.run(self._runner.run(query))
                    return result.output

                async def _arun(self, query: str) -> str:
                    result = await self._runner.run(query)
                    return result.output

            return _KrnlAILangChainTool()
        except ImportError:
            raise ImportError("langchain package required: pip install krnlai[langchain]")


class KrnlAIMemory:
    def __init__(self, runner: CognitiveCycleRunner) -> None:
        self._runner = runner

    def load_memory_variables(self, inputs: Dict[str, Any]) -> Dict[str, Any]:
        episodes = self._runner.episodic_memory.recent(10)
        return {
            "recent_episodes": [
                {"content": e.content, "type": e.episode_type} for e in episodes
            ],
            "working_memory": self._runner.working_memory.contents,
        }

    def save_context(self, inputs: Dict[str, Any], outputs: Dict[str, Any]) -> None:
        self._runner.working_memory.store(
            {"inputs": inputs, "outputs": outputs},
            ttl_seconds=120,
        )

    def clear(self) -> None:
        self._runner.working_memory.clear()
        self._runner.episodic_memory.clear()
