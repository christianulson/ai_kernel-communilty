from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any, AsyncGenerator, Dict, List, Optional


@dataclass
class LLMMessage:
    role: str = "user"
    content: str = ""
    name: Optional[str] = None


@dataclass
class LLMResponse:
    content: str = ""
    model: str = ""
    usage: Dict[str, int] = field(default_factory=dict)
    finish_reason: str = "stop"
    metadata: Dict[str, Any] = field(default_factory=dict)


class ILLMProvider(ABC):
    @abstractmethod
    async def complete(self, messages: List[LLMMessage], **kwargs: Any) -> LLMResponse:
        ...

    @abstractmethod
    async def stream_complete(self, messages: List[LLMMessage], **kwargs: Any) -> AsyncGenerator[str, None]:
        ...

    @abstractmethod
    @property
    def model_name(self) -> str:
        ...

    async def complete_str(self, prompt: str, **kwargs: Any) -> str:
        msg = LLMMessage(role="user", content=prompt)
        response = await self.complete([msg], **kwargs)
        return response.content
