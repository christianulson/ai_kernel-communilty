from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from typing import Dict, List


class BiasType(str, Enum):
    CONFIRMATION = "confirmation_bias"
    ANCHORING = "anchoring"
    AVAILABILITY = "availability"
    OVERCONFIDENCE = "overconfidence"
    FRAMING = "framing"
    HINDSIGHT = "hindsight"
    RECENCY = "recency"
    NEGATIVITY = "negativity"
    AFFECT_HEURISTIC = "affect_heuristic"
    GROUP_THINK = "group_think"


@dataclass
class BiasFlag:
    bias_type: BiasType
    severity: float
    evidence: str
    location: str = ""


CONFIRMATION_PATTERNS = [
    "as i thought", "as expected", "i knew", "clearly shows",
    "proves that", "obviously", "always", "never",
]

ANCHORING_PATTERNS = [
    "based on", "considering that", "given that", "according to",
    "first impression", "initial assessment",
]

AVAILABILITY_PATTERNS = [
    "recall", "remember", "recent", "last time", "example",
    "typical case", "common",
]

OVERCONFIDENCE_PATTERNS = [
    "definitely", "absolutely", "certainly", "without a doubt",
    "guaranteed", "undoubtedly", "i am sure",
]

FRAMING_PATTERNS_POSITIVE = [
    "gain", "benefit", "opportunity", "advantage", "improve",
]

FRAMING_PATTERNS_NEGATIVE = [
    "risk", "loss", "danger", "threat", "cost", "downside",
]

HINDSIGHT_PATTERNS = [
    "i knew it all along", "should have known", "obvious in hindsight",
    "could have predicted", "saw it coming", "inevitable",
]

RECENCY_PATTERNS = [
    "recent", "latest", "just happened", "new", "fresh",
]

NEGATIVITY_PATTERNS = [
    "problem", "issue", "error", "failure", "risk",
    "dangerous", "harmful", "threat", "bad", "worst",
]

AFFECT_PATTERNS = [
    "feel", "feeling", "emotion", "gut", "instinct",
    "intuition", "vibe", "sense",
]

GROUP_THINK_PATTERNS = [
    "everyone agrees", "consensus", "everyone thinks", "we all",
    "no one disagrees", "unanimous", "team decision",
]


class BiasDetector:
    def __init__(self) -> None:
        self._patterns: dict[BiasType, tuple[list[str], float]] = {
            BiasType.CONFIRMATION: (CONFIRMATION_PATTERNS, 0.6),
            BiasType.ANCHORING: (ANCHORING_PATTERNS, 0.5),
            BiasType.AVAILABILITY: (AVAILABILITY_PATTERNS, 0.5),
            BiasType.OVERCONFIDENCE: (OVERCONFIDENCE_PATTERNS, 0.7),
            BiasType.FRAMING: (FRAMING_PATTERNS_POSITIVE + FRAMING_PATTERNS_NEGATIVE, 0.4),
            BiasType.HINDSIGHT: (HINDSIGHT_PATTERNS, 0.6),
            BiasType.RECENCY: (RECENCY_PATTERNS, 0.4),
            BiasType.NEGATIVITY: (NEGATIVITY_PATTERNS, 0.5),
            BiasType.AFFECT_HEURISTIC: (AFFECT_PATTERNS, 0.5),
            BiasType.GROUP_THINK: (GROUP_THINK_PATTERNS, 0.6),
        }

    def detect(self, text: str, context: Dict | None = None) -> List[BiasFlag]:
        text_lower = text.lower()
        flags: list[BiasFlag] = []

        for bias_type, (patterns, base_severity) in self._patterns.items():
            matches = [p for p in patterns if p in text_lower]
            if matches:
                severity = min(1.0, base_severity + len(matches) * 0.1)
                flags.append(BiasFlag(
                    bias_type=bias_type,
                    severity=round(severity, 2),
                    evidence=matches[0],
                    location=bias_type.value,
                ))

        return flags
