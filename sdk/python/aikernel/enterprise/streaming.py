from __future__ import annotations

import json
import os
from typing import AsyncGenerator, Dict

import httpx

from krnlai.core.models.cognitive import CycleEvent


class EnterpriseStreamingClient:
    def __init__(
        self,
        endpoint: str = "",
        api_key: str = "",
    ) -> None:
        self._endpoint = (endpoint or os.getenv("AIKERNEL_ENDPOINT", "http://localhost:5001")).rstrip("/")
        self._api_key = api_key or os.getenv("AIKERNEL_API_KEY", "")

    def _headers(self) -> Dict[str, str]:
        headers = {"Accept": "text/event-stream"}
        if self._api_key:
            headers["Authorization"] = f"Bearer {self._api_key}"
        return headers

    async def stream_cycle(self, command: str) -> AsyncGenerator[CycleEvent, None]:
        async with httpx.AsyncClient() as client:
            async with client.stream(
                "POST",
                f"{self._endpoint}/api/v1/cycle/stream",
                json={"payload": command},
                headers=self._headers(),
                timeout=60,
            ) as response:
                response.raise_for_status()
                async for line in response.aiter_lines():
                    if line.startswith("data: "):
                        try:
                            data = json.loads(line[6:])
                            yield CycleEvent(**data)
                        except (json.JSONDecodeError, Exception):
                            pass
