from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable, Dict, List, Optional


@dataclass
class PolicyRule:
    id: str = ""
    name: str = ""
    condition: Optional[Callable[[Dict[str, Any]], bool]] = None
    action: Optional[Callable[[Dict[str, Any]], Dict[str, Any]]] = None
    priority: int = 0
    enabled: bool = True

    def evaluate(self, context: Dict[str, Any]) -> bool:
        if not self.enabled or self.condition is None:
            return False
        return self.condition(context)

    def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        if self.action is None:
            return context
        return self.action(context)


class PolicyEngine:
    def __init__(self) -> None:
        self._rules: List[PolicyRule] = []

    def add_rule(self, rule: PolicyRule) -> None:
        self._rules.append(rule)
        self._rules.sort(key=lambda r: r.priority, reverse=True)

    def evaluate(self, context: Dict[str, Any]) -> List[PolicyRule]:
        return [rule for rule in self._rules if rule.evaluate(context)]

    def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        result = dict(context)
        triggered = self.evaluate(result)
        for rule in triggered:
            result = rule.execute(result)
        return result

    def enable_rule(self, rule_id: str) -> None:
        for rule in self._rules:
            if rule.id == rule_id:
                rule.enabled = True

    def disable_rule(self, rule_id: str) -> None:
        for rule in self._rules:
            if rule.id == rule_id:
                rule.enabled = False

    @property
    def rules(self) -> List[PolicyRule]:
        return list(self._rules)
