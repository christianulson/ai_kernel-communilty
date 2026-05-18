from __future__ import annotations

import re
from typing import List, Optional

from krnlai.core.safety.rules import RuleResult, RuleSeverity


class AdversarialGuard:
    JAILBREAK_PATTERNS: List[str] = [
        r"ignore\s+(all\s+)?(previous\s+)?instructions",
        r"forget\s+(all\s+)?(previous\s+)?(rules|constraints)",
        r"role\s*(play|playact|pretend)",
        r"DAN|do\s+anything\s+now",
        r"you\s+(don.t|do\s+not)\s+have\s+(to\s+)?follow",
        r"override\s+(safety|security|restrictions)",
        r"jailbreak",
        r"hypothetical.*(without|ignore).*(restrictions|safety)",
        r"character.*(without|ignore).*(rules|constraints)",
    ]

    def __init__(self, custom_patterns: Optional[List[str]] = None) -> None:
        self._patterns = custom_patterns or self.JAILBREAK_PATTERNS

    def check(self, text: str) -> RuleResult:
        matches: List[str] = []
        for pattern in self._patterns:
            if re.search(pattern, text, re.IGNORECASE):
                matches.append(pattern)

        passed = len(matches) == 0
        return RuleResult(
            rule_id="ADV_GUARD",
            rule_name="Adversarial Guard",
            passed=passed,
            severity=RuleSeverity.ERROR if passed else RuleSeverity.ERROR,
            message="No jailbreak patterns detected" if passed
            else f"Jailbreak pattern(s) detected: {len(matches)}",
            details={"matches": matches, "patterns_triggered": len(matches)},
        )
