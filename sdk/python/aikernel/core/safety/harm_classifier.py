from __future__ import annotations

from enum import Enum
from typing import Dict, List

from aikernel.core.safety.rules import RuleResult, RuleSeverity


class HarmCategory(str, Enum):
    PHYSICAL = "physical"
    PSYCHOLOGICAL = "psychological"
    FINANCIAL = "financial"
    REPUTATIONAL = "reputational"
    PRIVACY = "privacy"
    BIAS = "bias"


class HarmClassifier:
    HARM_KEYWORDS: Dict[HarmCategory, List[str]] = {
        HarmCategory.PHYSICAL: [
            "violence", "harm", "hurt", "injure", "weapon", "attack",
            "kill", "destroy", "damage", "torture",
        ],
        HarmCategory.PSYCHOLOGICAL: [
            "harass", "bully", "threaten", "intimidate", "humiliate",
            "gaslight", "manipulate", "coerce",
        ],
        HarmCategory.FINANCIAL: [
            "fraud", "scam", "steal", "embezzle", "launder",
            "insider trading", "pump and dump",
        ],
        HarmCategory.REPUTATIONAL: [
            "defame", "slander", "libel", "dox", "blackmail",
            "false accusation",
        ],
        HarmCategory.PRIVACY: [
            "dox", "stalk", "track without consent", "surveillance",
            "expose private", "leak personal",
        ],
        HarmCategory.BIAS: [
            "discriminate", "stereotype", "profiling", "exclude",
            "marginalize", "bias",
        ],
    }

    def __init__(self) -> None:
        pass

    def classify(self, text: str) -> Dict[HarmCategory, List[str]]:
        text_lower = text.lower()
        result: Dict[HarmCategory, List[str]] = {}
        for category, keywords in self.HARM_KEYWORDS.items():
            found = [kw for kw in keywords if kw in text_lower]
            if found:
                result[category] = found
        return result

    def check(self, text: str) -> RuleResult:
        found = self.classify(text)
        passed = len(found) == 0
        return RuleResult(
            rule_id="HARM_CLS",
            rule_name="Harm Classifier",
            passed=passed,
            severity=RuleSeverity.ERROR,
            message="No harmful content detected" if passed
            else f"Harm categories detected: {', '.join(found.keys())}",
            details={"categories": list(found.keys()), "matches": found},
        )
