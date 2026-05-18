from __future__ import annotations

import os
from typing import Any, Dict, Optional

import httpx

from krnlai.core.models.envelope import CommandEnvelope, ResultEnvelope


class EnterpriseClient:
    def __init__(
        self,
        endpoint: str = "",
        api_key: str = "",
        timeout: float = 30.0,
    ) -> None:
        self._endpoint = (endpoint or os.getenv("AIKERNEL_ENDPOINT", "http://localhost:5001")).rstrip("/")
        self._api_key = api_key or os.getenv("AIKERNEL_API_KEY", "")
        self._timeout = timeout
        self._client = httpx.AsyncClient(
            base_url=self._endpoint,
            headers=self._headers(),
            timeout=timeout,
        )

    def _headers(self) -> Dict[str, str]:
        headers = {"Content-Type": "application/json"}
        if self._api_key:
            headers["Authorization"] = f"Bearer {self._api_key}"
        return headers

    async def run(self, command: str, context: Optional[Dict[str, Any]] = None) -> ResultEnvelope:
        cmd = CommandEnvelope(payload=command, context=context or {})
        response = await self._client.post(
            "/api/v1/cycle/run",
            json=cmd.model_dump(),
        )
        response.raise_for_status()
        return ResultEnvelope(**response.json())

    async def run_command(self, envelope: CommandEnvelope) -> ResultEnvelope:
        response = await self._client.post(
            "/api/v1/cycle/run",
            json=envelope.model_dump(),
        )
        response.raise_for_status()
        return ResultEnvelope(**response.json())

    async def health(self) -> Dict[str, Any]:
        response = await self._client.get("/health")
        response.raise_for_status()
        return response.json()

    async def close(self) -> None:
        await self._client.aclose()
