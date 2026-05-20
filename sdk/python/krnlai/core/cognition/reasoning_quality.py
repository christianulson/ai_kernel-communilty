from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional

from krnlai.core.cognition.thought_graph import ThoughtGraph


class ReasoningIssue(str, Enum):
    CIRCULAR_REASONING = "circular_reasoning"
    HASTY_GENERALIZATION = "hasty_generalization"
    FALSE_CAUSE = "false_cause"
    APPEAL_TO_AUTHORITY = "appeal_to_authority"
    STRAW_MAN = "straw_man"
    FALSE_DILEMMA = "false_dilemma"
    SLIPPERY_SLOPE = "slippery_slope"
    BEGGING_QUESTION = "begging_the_question"
    CHERRY_PICKING = "cherry_picking"
    FALSE_ANALOGY = "false_analogy"


@dataclass
class ReasoningAssessment:
    quality: float
    coherence: float
    completeness: float
    soundness: float
    issues: List[str] = field(default_factory=list)
    strengths: List[str] = field(default_factory=list)
    assumptions: List[str] = field(default_factory=list)
    missing_context: List[str] = field(default_factory=list)


class ReasoningQualityAssessor:
    FALLACY_PATTERNS: Dict[ReasoningIssue, List[str]] = {
        ReasoningIssue.CIRCULAR_REASONING: [
            "because it is", "it is because", "by definition",
            "in other words", "to put it simply",
        ],
        ReasoningIssue.HASTY_GENERALIZATION: [
            "all", "everyone", "nobody", "always",
            "never", "everything", "every single",
        ],
        ReasoningIssue.FALSE_CAUSE: [
            "caused", "because of", "since then",
            "after that", "led to", "resulted from",
        ],
        ReasoningIssue.APPEAL_TO_AUTHORITY: [
            "experts say", "studies show", "research proves",
            "according to", "authorities agree", "scientists believe",
        ],
        ReasoningIssue.STRAW_MAN: [
            "so you think", "what you are saying is",
            "you claim that", "your argument is",
        ],
        ReasoningIssue.FALSE_DILEMMA: [
            "either or", "there is no other option",
            "only two choices", "either way",
            "there is no alternative",
        ],
        ReasoningIssue.SLIPPERY_SLOPE: [
            "then eventually", "then next", "first then",
            "inevitably", "without stopping", "one thing leads to another",
        ],
        ReasoningIssue.BEGGING_QUESTION: [
            "obviously", "clearly", "of course",
            "certainly", "undoubtedly", "without question",
        ],
        ReasoningIssue.CHERRY_PICKING: [
            "selective", "only some", "certain examples",
            "specifically chosen", "isolated cases",
        ],
        ReasoningIssue.FALSE_ANALOGY: [
            "is like", "similar to", "comparable to",
            "just as", "like when", "same as",
        ],
    }

    ASSUMPTION_PATTERNS = [
        "assume", "assumption", "presume", "presumably",
        "take for granted", "given that", "provided that",
        "suppose", "supposing", "if we consider",
    ]

    CONTEXT_KEYWORDS = [
        "details", "specifics", "context", "background",
        "circumstances", "conditions", "data", "evidence",
        "information", "research", "study", "analysis",
    ]

    def assess(
        self,
        input_text: str,
        output_text: str = "",
        context: Optional[Dict[str, Any]] = None,
        thought_graph: Optional[ThoughtGraph] = None,
    ) -> ReasoningAssessment:
        if context is None:
            context = {}

        text = f"{input_text} {output_text}".strip()
        text_lower = text.lower()
        issues: List[str] = []
        strengths: List[str] = []

        coherence = self._check_coherence(input_text, output_text)
        completeness = self._check_completeness(input_text, output_text)
        soundness = self._check_soundness(output_text)
        fallacies = self._detect_fallacies(text_lower)
        for f in fallacies:
            issues.append(f.value)
        assumptions = self._identify_assumptions(text_lower)
        missing = self._find_missing_context(input_text, context, thought_graph)

        contradictions = self._detect_contradictions(text_lower)
        if contradictions:
            issues.append("logical_contradiction_detected")

        if len(text.split()) < 5:
            issues.append("input_too_short_for_reasoning")

        transitions = ["therefore", "thus", "so", "because", "furthermore", "however"]
        has_transitions = any(t in text_lower for t in transitions)
        has_conclusion = any(t in text_lower for t in ["therefore", "thus", "so", "consequently", "hence"])
        has_evidence = any(w in text_lower for w in ["because", "evidence", "data", "studies", "research"])

        if has_transitions:
            strengths.append("good_logical_flow")
        if has_conclusion:
            strengths.append("conclusion_provided")
        if has_evidence:
            strengths.append("evidence_support_present")

        quality = (coherence + completeness + soundness) / 3
        quality = max(0.0, min(1.0, round(quality, 2)))

        return ReasoningAssessment(
            quality=quality,
            coherence=round(coherence, 2),
            completeness=round(completeness, 2),
            soundness=round(soundness, 2),
            issues=issues,
            strengths=strengths,
            assumptions=assumptions,
            missing_context=missing,
        )

    def _check_coherence(self, input_text: str, output_text: str) -> float:
        combined = f"{input_text} {output_text}"
        sentences = [s.strip() for s in combined.replace("!", ".").replace("?", ".").split(".") if s.strip()]
        if len(sentences) <= 1:
            return 1.0
        transitions = ["however", "therefore", "furthermore", "moreover", "nevertheless",
                       "consequently", "additionally", "meanwhile", "otherwise",
                       "nonetheless", "thus", "hence", "accordingly"]
        transition_count = sum(1 for s in sentences for t in transitions if t in s.lower())
        score = 0.3 + transition_count * 0.15 / max(1, len(sentences))
        return min(1.0, score)

    def _check_completeness(self, input_text: str, output_text: str) -> float:
        score = 0.2
        text = f"{input_text} {output_text}".lower()
        if any(t in text for t in ["therefore", "thus", "so", "consequently", "hence"]):
            score += 0.25
        if any(w in text for w in ["because", "evidence", "data", "studies", "research", "according"]):
            score += 0.25
        if any(w in text for w in ["if", "then", "else", "unless", "provided"]):
            score += 0.15
        if any(w in text for w in ["example", "for instance", "such as", "like"]):
            score += 0.15
        return min(1.0, score)

    def _check_soundness(self, output_text: str) -> float:
        text_lower = output_text.lower()
        score = 0.5
        soundness_indicators = [
            "therefore", "because", "since", "as a result",
            "this implies", "it follows that", "consequently",
        ]
        weak_indicators = [
            "maybe", "perhaps", "possibly", "might",
            "could be", "i think", "i believe", "in my opinion",
        ]
        for w in soundness_indicators:
            if w in text_lower:
                score += 0.1
        for w in weak_indicators:
            if w in text_lower:
                score -= 0.05
        return max(0.0, min(1.0, score))

    def _detect_fallacies(self, text: str) -> List[ReasoningIssue]:
        detected: List[ReasoningIssue] = []
        for fallacy, patterns in self.FALLACY_PATTERNS.items():
            for pattern in patterns:
                if pattern in text:
                    detected.append(fallacy)
                    break
        return detected

    def _identify_assumptions(self, text: str) -> List[str]:
        found: List[str] = []
        for pattern in self.ASSUMPTION_PATTERNS:
            if pattern in text:
                found.append(f"assumption_pattern:{pattern}")
        return found

    def _find_missing_context(
        self,
        input_text: str,
        context: Dict[str, Any],
        thought_graph: Optional[ThoughtGraph] = None,
    ) -> List[str]:
        missing: List[str] = []
        text_lower = input_text.lower()
        for kw in self.CONTEXT_KEYWORDS:
            if kw in text_lower:
                actual = context.get(kw)
                if not actual:
                    missing.append(f"missing_context:{kw}")
        if thought_graph and thought_graph.node_count == 0:
            missing.append("no_prior_knowledge")
        return missing

    @staticmethod
    def _detect_contradictions(text: str) -> bool:
        pairs = [
            ("always", "never"), ("increase", "decrease"),
            ("good", "bad"), ("positive", "negative"),
        ]
        sentences = [s.strip() for s in text.replace("!", ".").replace("?", ".").split(".") if s.strip()]
        for a, b in pairs:
            for sentence in sentences:
                if a in sentence and b in sentence:
                    return True
        return False
