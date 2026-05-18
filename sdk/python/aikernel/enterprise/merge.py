from __future__ import annotations

import os
from typing import Any, AsyncGenerator, Dict, Optional

from krnlai.core.cycle import CognitiveCycleRunner, CycleConfig
from krnlai.core.models.cognitive import CycleEvent
from krnlai.core.models.envelope import CommandEnvelope, ResultEnvelope
from krnlai.enterprise.client import EnterpriseClient


class CognitiveAgent:
    def __init__(
        self,
        mode: str = "auto",
        safety_level: str = "strict",
        endpoint: str = "",
        api_key: str = "",
        max_iterations: int = 10,
        enable_emotions: bool = True,
    ) -> None:
        self._mode = mode
        self._standalone: Optional[CognitiveCycleRunner] = None
        self._enterprise: Optional[EnterpriseClient] = None
        self._safety_level = safety_level
        self._endpoint = endpoint
        self._api_key = api_key
        self._max_iterations = max_iterations
        self._enable_emotions = enable_emotions

        if mode == "standalone":
            self._standalone = self._create_standalone()
        elif mode == "enterprise":
            self._enterprise = self._create_enterprise()
        elif mode == "auto":
            if self._detect_endpoint():
                self._enterprise = self._create_enterprise()
            else:
                self._standalone = self._create_standalone()

    def _create_standalone(self) -> CognitiveCycleRunner:
        config = CycleConfig(
            safety_level=self._safety_level,
            max_iterations=self._max_iterations,
            enable_emotions=self._enable_emotions,
        )
        return CognitiveCycleRunner(config=config)

    def _create_enterprise(self) -> EnterpriseClient:
        return EnterpriseClient(
            endpoint=self._endpoint or os.getenv("AIKERNEL_ENDPOINT", "http://localhost:5001"),
            api_key=self._api_key or os.getenv("AIKERNEL_API_KEY", ""),
        )

    def _detect_endpoint(self) -> bool:
        endpoint = self._endpoint or os.getenv("AIKERNEL_ENDPOINT", "")
        if not endpoint:
            return False
        try:
            import httpx
            resp = httpx.get(f"{endpoint.rstrip('/')}/health", timeout=2)
            return resp.status_code == 200
        except Exception:
            return False

    @property
    def is_standalone(self) -> bool:
        return self._standalone is not None

    @property
    def is_enterprise(self) -> bool:
        return self._enterprise is not None

    @property
    def mode(self) -> str:
        if self._standalone and self._enterprise:
            return "hybrid"
        if self._standalone:
            return "standalone"
        if self._enterprise:
            return "enterprise"
        return "unknown"

    async def run(self, command: str, context: Optional[Dict[str, Any]] = None) -> ResultEnvelope:
        if self._standalone:
            return await self._standalone.run(command, context)
        if self._enterprise:
            return await self._enterprise.run(command, context)
        raise RuntimeError("No mode available")

    async def run_command(self, envelope: CommandEnvelope) -> ResultEnvelope:
        if self._standalone:
            return await self._standalone.run_command(envelope)
        if self._enterprise:
            return await self._enterprise.run_command(envelope)
        raise RuntimeError("No mode available")

    async def stream(self, command: str) -> AsyncGenerator[CycleEvent, None]:
        if self._standalone:
            cmd = CommandEnvelope(payload=command, context={})
            async for event in self._standalone.stream_cycle(cmd):
                yield event
        elif self._enterprise:
            async for event in self._enterprise.stream_cycle(command):
                yield event
        else:
            raise RuntimeError("No mode available")

    async def close(self) -> None:
        if self._enterprise:
            await self._enterprise.close()
