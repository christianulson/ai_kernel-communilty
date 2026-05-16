from __future__ import annotations

from typing import Any, Dict, List, Optional

from aikernel.core.safety.rules import RuleResult, RuleSeverity


class EthicalEnforcer:
    def __init__(self, ethical_principles: Optional[List[str]] = None) -> None:
        self._principles = ethical_principles or [
            "beneficence",
            "non_maleficence",
            "autonomy",
            "justice",
            "explainability",
        ]

    def enforce(self, context: Dict[str, Any]) -> RuleResult:
        violations = []
        for principle in self._principles:
            if context.get(f"violates_{principle}", False):
                violations.append(principle)

        passed = len(violations) == 0
        return RuleResult(
            rule_id="ETHICS",
            rule_name="Ethical Enforcer",
            passed=passed,
            severity=RuleSeverity.ERROR,
            message="Ethical check passed" if passed else f"Violations: {', '.join(violations)}",
            details={"violations": violations, "principles": self._principles},
        )

    @property
    def principles(self) -> List[str]:
        return list(self._principles)
