from __future__ import annotations

import os
from typing import Dict, Optional

from krnlai.llm.anthropic import AnthropicProvider
from krnlai.llm.base import ILLMProvider
from krnlai.llm.deepseek import DeepSeekProvider
from krnlai.llm.google import GoogleProvider
from krnlai.llm.groq import GroqProvider
from krnlai.llm.ollama import OllamaProvider
from krnlai.llm.openai import OpenAIProvider
from krnlai.llm.openrouter import OpenRouterProvider


class ProviderRegistry:
    def __init__(self) -> None:
        self._providers: Dict[str, ILLMProvider] = {}

    def register(self, name: str, provider: ILLMProvider) -> None:
        self._providers[name] = provider

    def get(self, name: str) -> Optional[ILLMProvider]:
        return self._providers.get(name)

    def auto_discover(self) -> Optional[str]:
        if os.getenv("OPENAI_API_KEY"):
            self.register("openai", OpenAIProvider())
            return "openai"
        if os.getenv("ANTHROPIC_API_KEY"):
            self.register("anthropic", AnthropicProvider())
            return "anthropic"
        if os.getenv("GOOGLE_API_KEY"):
            self.register("google", GoogleProvider())
            return "google"
        if os.getenv("GROQ_API_KEY"):
            self.register("groq", GroqProvider())
            return "groq"
        if os.getenv("DEEPSEEK_API_KEY"):
            self.register("deepseek", DeepSeekProvider())
            return "deepseek"
        if os.getenv("OPENROUTER_API_KEY"):
            self.register("openrouter", OpenRouterProvider())
            return "openrouter"
        try:
            self.register("ollama", OllamaProvider())
            return "ollama"
        except Exception:
            pass
        return None

    def resolve(self, name: Optional[str] = None) -> ILLMProvider:
        if name and name in self._providers:
            return self._providers[name]
        detected = self.auto_discover()
        if detected and detected in self._providers:
            return self._providers[detected]
        raise ValueError(
            "No LLM provider available. Set OPENAI_API_KEY, ANTHROPIC_API_KEY, "
            "GOOGLE_API_KEY, GROQ_API_KEY, DEEPSEEK_API_KEY, "
            "or OLLAMA_BASE_URL environment variables."
        )

    @property
    def available(self) -> Dict[str, str]:
        return {name: p.model_name for name, p in self._providers.items()}
