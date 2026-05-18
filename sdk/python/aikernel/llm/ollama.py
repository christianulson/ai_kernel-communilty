from __future__ import annotations

import json
import os
from typing import Any, AsyncGenerator, List, Optional

import httpx

from krnlai.llm.base import ILLMProvider, LLMMessage, LLMResponse


class OllamaProvider(ILLMProvider):
    def __init__(self, model: str = "llama3", base_url: Optional[str] = None) -> None:
        self._model = model
        self._base_url = (base_url or os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")).rstrip("/")

    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{self._base_url}/api/chat",
                json={
                    "model": kwargs.get("model", self._model),
                    "messages": api_messages,
                    "stream": False,
                    "options": {
                        "temperature": kwargs.get("temperature", 0.7),
                        "num_predict": kwargs.get("max_tokens", 4096),
                    },
                },
                timeout=60,
            )
            response.raise_for_status()
            data = response.json()
            return LLMResponse(
                content=data.get("message", {}).get("content", ""),
                model=self._model,
                usage={},
                finish_reason="stop",
            )

    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        async with httpx.AsyncClient() as client:
            async with client.stream(
                "POST",
                f"{self._base_url}/api/chat",
                json={
                    "model": kwargs.get("model", self._model),
                    "messages": api_messages,
                    "stream": True,
                    "options": {
                        "temperature": kwargs.get("temperature", 0.7),
                        "num_predict": kwargs.get("max_tokens", 4096),
                    },
                },
                timeout=120,
            ) as response:
                async for line in response.aiter_lines():
                    if line.strip():
                        try:
                            chunk = json.loads(line)
                            content = chunk.get("message", {}).get("content", "")
                            if content:
                                yield content
                        except json.JSONDecodeError:
                            pass

    @property
    def model_name(self) -> str:
        return self._model
