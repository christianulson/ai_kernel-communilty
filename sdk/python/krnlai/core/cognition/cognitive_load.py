from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict


@dataclass
class CognitiveLoadAssessment:
    overall_load: float
    intrinsic_load: float
    extraneous_load: float
    germane_load: float


class CognitiveLoadAssessor:
    DOMAIN_KEYWORDS: dict[str, float] = {
        "code": 0.7, "algorithm": 0.8, "function": 0.6, "class": 0.6,
        "mathematics": 0.9, "analysis": 0.7, "system": 0.6,
        "architecture": 0.7, "science": 0.7, "philosophy": 0.8,
    }

    COMPLEXITY_INDICATORS = [
        "because", "however", "therefore", "although", "nevertheless",
        "consequently", "furthermore", "moreover", "alternatively",
    ]

    EXTRANEOUS_INDICATORS = [
        "unnecessary", "vague", "unclear", "confusing",
        "irrelevant", "distracting", "redundant",
    ]

    def assess(
        self,
        payload: str,
        context: Dict[str, Any] | None = None,
        homeostasis_state: Dict[str, Any] | None = None,
    ) -> CognitiveLoadAssessment:
        if context is None:
            context = {}
        if homeostasis_state is None:
            homeostasis_state = {}

        word_count = len(payload.split())
        text_lower = payload.lower()

        intrinsic = self._assess_intrinsic(text_lower, word_count)
        extraneous = self._assess_extraneous(text_lower, word_count)
        novelty = homeostasis_state.get("novelty", 0.0)
        fatigue = homeostasis_state.get("fatigue", 0.0)
        context_richness = len(context)

        germane = self._assess_germane(novelty, context_richness)

        overall = round(min(1.0, intrinsic * 0.5 + extraneous * 0.2 + germane * 0.2 + fatigue * 0.1), 2)

        return CognitiveLoadAssessment(
            overall_load=overall,
            intrinsic_load=round(intrinsic, 2),
            extraneous_load=round(extraneous, 2),
            germane_load=round(germane, 2),
        )

    def _assess_intrinsic(self, text: str, word_count: int) -> float:
        length_factor = min(1.0, word_count / 200.0)
        domain_score = 0.0
        for keyword, score in self.DOMAIN_KEYWORDS.items():
            if keyword in text:
                domain_score = max(domain_score, score)
        complexity_indicator_count = sum(1 for c in self.COMPLEXITY_INDICATORS if c in text)
        complexity_factor = min(1.0, complexity_indicator_count * 0.15)
        return max(0.1, length_factor * 0.4 + domain_score * 0.4 + complexity_factor * 0.2)

    def _assess_extraneous(self, text: str, word_count: int) -> float:
        word_freq = {w: text.split().count(w) for w in self.EXTRANEOUS_INDICATORS if w in text}
        redundancy_penalty = min(0.5, sum(word_freq.values()) * 0.1)
        length_penalty = min(0.3, max(0, word_count - 500) / 1000.0)
        return round(min(1.0, redundancy_penalty + length_penalty), 2)

    @staticmethod
    def _assess_germane(novelty: float, context_richness: int) -> float:
        schema_factor = min(1.0, context_richness / 10.0)
        return min(1.0, novelty * 0.6 + schema_factor * 0.4)
