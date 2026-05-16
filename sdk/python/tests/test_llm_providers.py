from __future__ import annotations

import pytest

from aikernel.llm.base import ILLMProvider, LLMMessage, LLMResponse
from aikernel.llm.deepseek import DeepSeekProvider
from aikernel.llm.google import GoogleProvider
from aikernel.llm.groq import GroqProvider
from aikernel.llm.registry import ProviderRegistry


class MockProvider(ILLMProvider):
    def __init__(self) -> None:
        self._model = "mock-model"

    async def complete(self, messages, **kwargs):
        return LLMResponse(content="mock response", model="mock-model")

    async def stream_complete(self, messages, **kwargs):
        yield "mock "
        yield "response"

    @property
    def model_name(self) -> str:
        return self._model


class TestProviderRegistry:
    def test_Register_And_Get(self):
        registry = ProviderRegistry()
        mock = MockProvider()
        registry.register("mock", mock)
        assert registry.get("mock") is mock

    def test_Get_NonExisting_ShouldReturnNone(self):
        registry = ProviderRegistry()
        assert registry.get("nonexistent") is None

    def test_Available_ShouldReturnRegistered(self):
        registry = ProviderRegistry()
        registry.register("mock", MockProvider())
        available = registry.available
        assert "mock" in available
        assert available["mock"] == "mock-model"

    def test_Resolve_Registered_ShouldReturn(self):
        registry = ProviderRegistry()
        mock = MockProvider()
        registry.register("mock", mock)
        assert registry.resolve("mock") is mock


class TestDeepSeekProvider:
    def test_ModelName_ShouldReturn(self):
        provider = DeepSeekProvider(model="deepseek-chat")
        assert provider.model_name == "deepseek-chat"


class TestGroqProvider:
    def test_ModelName_ShouldReturn(self):
        provider = GroqProvider(model="llama3-70b-8192")
        assert provider.model_name == "llama3-70b-8192"


class TestGoogleProvider:
    def test_ModelName_ShouldReturn(self):
        provider = GoogleProvider(model="gemini-2.0-flash")
        assert provider.model_name == "gemini-2.0-flash"


class TestMockProvider:
    @pytest.mark.asyncio
    async def test_Complete_ShouldReturnContent(self):
        provider = MockProvider()
        response = await provider.complete([LLMMessage(content="hello")])
        assert response.content == "mock response"
        assert response.model == "mock-model"

    @pytest.mark.asyncio
    async def test_StreamComplete_ShouldYieldChunks(self):
        provider = MockProvider()
        chunks = []
        async for chunk in provider.stream_complete([LLMMessage(content="hello")]):
            chunks.append(chunk)
        assert "".join(chunks) == "mock response"
