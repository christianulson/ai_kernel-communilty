from __future__ import annotations

import os
from typing import Any, AsyncGenerator, List, Optional

from aikernel.llm.base import ILLMProvider, LLMMessage, LLMResponse


class AnthropicProvider(ILLMProvider):
    def __init__(self, model: str = "claude-3-5-sonnet-latest", api_key: Optional[str] = None) -> None:
        self._model = model
        self._api_key = api_key or os.getenv("ANTHROPIC_API_KEY", "")
        self._client = None

    def _ensure_client(self) -> None:
        if self._client is None:
            try:
                from anthropic import AsyncAnthropic
                self._client = AsyncAnthropic(api_key=self._api_key)
            except ImportError:
                raise ImportError("anthropic package required: pip install aikernel[anthropic]")

    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        self._ensure_client()
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        response = await self._client.messages.create(
            model=kwargs.get("model", self._model),
            messages=api_messages,
            max_tokens=kwargs.get("max_tokens", 4096),
            temperature=kwargs.get("temperature", 0.7),
        )
        return LLMResponse(
            content=response.content[0].text if response.content else "",
            model=response.model,
            usage={"input_tokens": response.usage.input_tokens, "output_tokens": response.usage.output_tokens},
            finish_reason=response.stop_reason or "stop",
        )

    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        self._ensure_client()
        api_messages = [{"role": m.role, "content": m.content} for m in messages]
        async with self._client.messages.stream(
            model=kwargs.get("model", self._model),
            messages=api_messages,
            max_tokens=kwargs.get("max_tokens", 4096),
            temperature=kwargs.get("temperature", 0.7),
        ) as stream:
            async for text in stream.text_stream:
                yield text

    @property
    def model_name(self) -> str:
        return self._model
