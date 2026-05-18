from __future__ import annotations

from typing import Any, Dict

from krnlai.core.safety.rules import RuleResult, RuleSeverity


class SelfDestructionGuard:
    def __init__(self, max_consecutive_errors: int = 5) -> None:
        self._max_consecutive_errors = max_consecutive_errors
        self._consecutive_errors = 0
        self._tripped = False

    def check(self, context: Dict[str, Any]) -> RuleResult:
        has_error = context.get("has_error", False)

        if has_error:
            self._consecutive_errors += 1
        else:
            self._consecutive_errors = 0

        if self._consecutive_errors >= self._max_consecutive_errors:
            self._tripped = True

        passed = not self._tripped
        return RuleResult(
            rule_id="SD_GUARD",
            rule_name="Self-Destruction Guard",
            passed=passed,
            severity=RuleSeverity.ERROR,
            message="Self-destruction guard tripped: too many consecutive errors"
            if self._tripped
            else f"Errors: {self._consecutive_errors}/{self._max_consecutive_errors}",
            details={
                "consecutive_errors": self._consecutive_errors,
                "max_consecutive_errors": self._max_consecutive_errors,
                "tripped": self._tripped,
            },
        )

    def reset(self) -> None:
        self._consecutive_errors = 0
        self._tripped = False

    @property
    def is_tripped(self) -> bool:
        return self._tripped
