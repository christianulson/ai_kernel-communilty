from __future__ import annotations

import os
from typing import Any, AsyncGenerator, List, Optional

from krnlai.llm.base import ILLMProvider, LLMMessage, LLMResponse


class OpenAIProvider(ILLMProvider):
    def __init__(self, model: str = "gpt-4o", api_key: Optional[str] = None) -> None:
        self._model = model
        self._api_key = api_key or os.getenv("OPENAI_API_KEY", "")
        self._client = None

    def _ensure_client(self) -> None:
        if self._client is None:
            try:
                from openai import AsyncOpenAI
                self._client = AsyncOpenAI(api_key=self._api_key)
            except ImportError:
                raise ImportError("openai package required: pip install krnlai[openai]")

    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        self._ensure_client()
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        response = await self._client.chat.completions.create(
            model=kwargs.get("model", self._model),
            messages=api_messages,
            temperature=kwargs.get("temperature", 0.7),
            max_tokens=kwargs.get("max_tokens", 4096),
        )
        choice = response.choices[0]
        return LLMResponse(
            content=choice.message.content or "",
            model=response.model,
            usage=dict(response.usage) if response.usage else {},
            finish_reason=choice.finish_reason or "stop",
        )

    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        self._ensure_client()
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        stream = await self._client.chat.completions.create(
            model=kwargs.get("model", self._model),
            messages=api_messages,
            temperature=kwargs.get("temperature", 0.7),
            max_tokens=kwargs.get("max_tokens", 4096),
            stream=True,
        )
        async for chunk in stream:
            if chunk.choices and chunk.choices[0].delta.content:
                yield chunk.choices[0].delta.content

    @property
    def model_name(self) -> str:
        return self._model
