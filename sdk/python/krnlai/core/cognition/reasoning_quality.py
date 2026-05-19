from __future__ import annotations

from dataclasses import dataclass, field
from typing import Dict, List


@dataclass
class ReasoningAssessment:
    quality: float
    issues: List[str] = field(default_factory=list)
    strengths: List[str] = field(default_factory=list)
    coherence: float = 1.0
    completeness: float = 1.0


class ReasoningQualityAssessor:
    def assess(self, text: str, context: Dict | None = None) -> ReasoningAssessment:
        if context is None:
            context = {}
        text_lower = text.lower()
        issues: list[str] = []
        strengths: list[str] = []

        words = text.split()
        word_count = len(words)
        sentences = [s.strip() for s in text.replace("!", ".").replace("?", ".").split(".") if s.strip()]

        if word_count < 5:
            issues.append("input_too_short_for_reasoning")
        elif word_count > 5:
            strengths.append("sufficient_length_for_analysis")

        has_contradictions = self._detect_contradictions(text_lower)
        if has_contradictions:
            issues.append("logical_contradiction_detected")

        has_conditionals = any(w in text_lower for w in ["if", "then", "else", "unless", "provided"])
        has_conclusions = any(w in text_lower for w in ["therefore", "thus", "so", "consequently", "because", "hence"])
        evidence_words = ["because", "since", "as", "due to", "according to", "evidence", "data", "studies"]
        has_evidence = any(w in text_lower for w in evidence_words)

        if has_conditionals:
            strengths.append("conditional_reasoning_used")
        if has_conclusions:
            strengths.append("conclusion_provided")
        if has_evidence:
            strengths.append("evidence_support_present")

        if not has_conclusions:
            issues.append("missing_explicit_conclusion")
        if not has_conditionals and not has_evidence:
            issues.append("limited_reasoning_structure")

        coherence = self._compute_coherence(sentences)
        completeness = self._compute_completeness(has_conclusions, has_evidence, has_conditionals)

        ratio = len(strengths) / max(1, len(strengths) + len(issues))
        quality = round(coherence * 0.4 + completeness * 0.4 + ratio * 0.2, 2)
        quality = max(0.0, min(1.0, quality))

        return ReasoningAssessment(
            quality=quality,
            issues=issues,
            strengths=strengths,
            coherence=round(coherence, 2),
            completeness=round(completeness, 2),
        )

    @staticmethod
    def _detect_contradictions(text: str) -> bool:
        contradiction_pairs = [
            ("always", "never"),
            ("yes", "no"),
            ("increase", "decrease"),
            ("good", "bad"),
            ("positive", "negative"),
        ]
        for a, b in contradiction_pairs:
            if a in text and b in text:
                return True
        return False

    @staticmethod
    def _compute_coherence(sentences: list[str]) -> float:
        if len(sentences) <= 1:
            return 1.0
        transitions = ["however", "therefore", "furthermore", "moreover", "nevertheless",
                       "consequently", "additionally", "meanwhile", "otherwise",
                       "nonetheless", "thus", "hence", "accordingly"]
        transition_count = sum(1 for s in sentences for t in transitions if t in s.lower())
        return min(1.0, 0.3 + transition_count * 0.15 / max(1, len(sentences)))

    @staticmethod
    def _compute_completeness(has_conclusion: bool, has_evidence: bool, has_conditionals: bool) -> float:
        score = 0.3
        if has_conclusion:
            score += 0.3
        if has_evidence:
            score += 0.2
        if has_conditionals:
            score += 0.2
        return min(1.0, score)
