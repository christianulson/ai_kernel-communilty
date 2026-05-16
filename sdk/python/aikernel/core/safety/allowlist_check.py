from __future__ import annotations

from typing import Any, Dict, List, Optional

from aikernel.core.safety.rules import RuleResult, RuleSeverity


class AllowlistCheck:
    def __init__(self, allowed_actions: Optional[List[str]] = None) -> None:
        self._allowed_actions = allowed_actions or ["kernel.handle"]

    def check(self, context: Dict[str, Any]) -> RuleResult:
        action = context.get("action", "")
        passed = action in self._allowed_actions
        return RuleResult(
            rule_id="ALLOWLIST",
            rule_name="Allowlist Check",
            passed=passed,
            severity=RuleSeverity.ERROR,
            message=f"Action '{action}' is {'allowed' if passed else 'blocked by allowlist'}",
            details={
                "action": action,
                "allowed_actions": list(self._allowed_actions),
            },
        )

    def add_action(self, action: str) -> None:
        self._allowed_actions.append(action)

    def remove_action(self, action: str) -> None:
        self._allowed_actions = [a for a in self._allowed_actions if a != action]
