from __future__ import annotations

import os
from typing import Any, AsyncGenerator, List, Optional

from aikernel.llm.base import ILLMProvider, LLMMessage, LLMResponse


class GoogleProvider(ILLMProvider):
    def __init__(self, model: str = "gemini-2.0-flash", api_key: Optional[str] = None) -> None:
        self._model = model
        self._api_key = api_key or os.getenv("GOOGLE_API_KEY", "")
        self._client = None

    def _ensure_client(self) -> None:
        if self._client is None:
            try:
                import google.generativeai as genai
                genai.configure(api_key=self._api_key)
                self._client = genai
            except ImportError:
                raise ImportError("google-generativeai package required: pip install google-generativeai")

    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        self._ensure_client()
        model = self._client.GenerativeModel(kwargs.get("model", self._model))
        prompt = self._build_prompt(messages)
        response = await model.generate_content_async(
            prompt,
            generation_config={
                "temperature": kwargs.get("temperature", 0.7),
                "max_output_tokens": kwargs.get("max_tokens", 4096),
            },
        )
        return LLMResponse(
            content=response.text or "",
            model=self._model,
            usage={},
            finish_reason="stop",
        )

    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        self._ensure_client()
        model = self._client.GenerativeModel(kwargs.get("model", self._model))
        prompt = self._build_prompt(messages)
        stream = await model.generate_content_async(
            prompt,
            generation_config={
                "temperature": kwargs.get("temperature", 0.7),
                "max_output_tokens": kwargs.get("max_tokens", 4096),
            },
            stream=True,
        )
        async for chunk in stream:
            if chunk.text:
                yield chunk.text

    def _build_prompt(self, messages: List[LLMMessage]) -> str:
        parts: List[str] = []
        for msg in messages:
            prefix = f"{msg.role}: " if msg.role != "user" else ""
            parts.append(f"{prefix}{msg.content}")
        return "\n".join(parts)

    @property
    def model_name(self) -> str:
        return self._model
