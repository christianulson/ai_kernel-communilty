from __future__ import annotations

from typing import Any, Dict, List

from aikernel.core.models.safety import RiskLevel
from aikernel.core.safety.rules import (
    RULES_REGISTRY,
    FundamentalRule,
    RuleResult,
    RuleSeverity,
    SafetyVerdictData,
)


class FundamentalRulesEngine:
    def __init__(self, rules: List[FundamentalRule]) -> None:
        self._rules = rules

    @classmethod
    def with_all_rules(cls) -> FundamentalRulesEngine:
        return cls(rules=list(RULES_REGISTRY))

    @classmethod
    def with_core_rules(cls) -> FundamentalRulesEngine:
        core_ids = {"R01", "R02", "R03", "R04", "R10", "R11", "R16", "R19", "R20"}
        core_rules = [r for r in RULES_REGISTRY if r.id in core_ids]
        return cls(rules=core_rules)

    def evaluate(self, context: Dict[str, Any]) -> List[RuleResult]:
        return [rule.evaluate(context) for rule in self._rules]

    def evaluate_verdict(self, context: Dict[str, Any]) -> SafetyVerdictData:
        results = self.evaluate(context)
        blocked_by = [
            r.rule_id for r in results
            if not r.passed and r.severity == RuleSeverity.ERROR
        ]
        risk_score = sum(1 for r in results if not r.passed) / max(len(results), 1)
        risk_level = (
            RiskLevel.CRITICAL if risk_score > 0.7
            else RiskLevel.HIGH if risk_score > 0.4
            else RiskLevel.MEDIUM if risk_score > 0.1
            else RiskLevel.LOW
        )
        return SafetyVerdictData(
            allowed=len(blocked_by) == 0,
            risk_level=risk_level,
            risk_score=risk_score,
            rule_results=results,
            blocked_by=blocked_by,
            requires_approval=risk_score > 0.3,
            reason="Blocked by: " + ", ".join(blocked_by) if blocked_by else "All rules passed",
        )

    def get_rule(self, rule_id: str) -> FundamentalRule:
        for rule in self._rules:
            if rule.id == rule_id:
                return rule
        raise ValueError(f"Rule {rule_id} not found")
