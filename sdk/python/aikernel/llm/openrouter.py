from __future__ import annotations

import os
from typing import Any, AsyncGenerator, List, Optional

import httpx

from aikernel.llm.base import ILLMProvider, LLMMessage, LLMResponse


class OpenRouterProvider(ILLMProvider):
    def __init__(self, model: str = "openai/gpt-4o", api_key: Optional[str] = None) -> None:
        self._model = model
        self._api_key = api_key or os.getenv("OPENROUTER_API_KEY", "")
        self._base_url = "https://openrouter.ai/api/v1"

    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        async with httpx.AsyncClient() as client:
            response = await client.post(
                f"{self._base_url}/chat/completions",
                headers={
                    "Authorization": f"Bearer {self._api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": kwargs.get("model", self._model),
                    "messages": api_messages,
                    "temperature": kwargs.get("temperature", 0.7),
                    "max_tokens": kwargs.get("max_tokens", 4096),
                },
                timeout=60,
            )
            response.raise_for_status()
            data = response.json()
            choice = data["choices"][0]
            return LLMResponse(
                content=choice["message"]["content"],
                model=data.get("model", self._model),
                usage=data.get("usage", {}),
                finish_reason=choice.get("finish_reason", "stop"),
            )

    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        async with httpx.AsyncClient() as client:
            async with client.stream(
                "POST",
                f"{self._base_url}/chat/completions",
                headers={
                    "Authorization": f"Bearer {self._api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": kwargs.get("model", self._model),
                    "messages": api_messages,
                    "temperature": kwargs.get("temperature", 0.7),
                    "max_tokens": kwargs.get("max_tokens", 4096),
                    "stream": True,
                },
                timeout=120,
            ) as response:
                async for line in response.aiter_lines():
                    if line.startswith("data: ") and line != "data: [DONE]":
                        import json
                        try:
                            chunk = json.loads(line[6:])
                            delta = chunk.get("choices", [{}])[0].get("delta", {})
                            content = delta.get("content", "")
                            if content:
                                yield content
                        except json.JSONDecodeError:
                            pass

    @property
    def model_name(self) -> str:
        return self._model
