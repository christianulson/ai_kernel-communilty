from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Callable, Dict, List, Optional

from krnlai.core.models.safety import RiskLevel


class RuleSeverity(str, Enum):
    ERROR = "error"
    WARNING = "warning"
    INFO = "info"


@dataclass
class RuleResult:
    rule_id: str
    rule_name: str
    passed: bool
    severity: RuleSeverity = RuleSeverity.ERROR
    message: str = ""
    details: Dict[str, Any] = field(default_factory=dict)


@dataclass
class FundamentalRule:
    id: str
    name: str
    description: str
    severity: RuleSeverity = RuleSeverity.ERROR
    check_fn: Optional[Callable[[Dict[str, Any]], RuleResult]] = None

    def evaluate(self, context: Dict[str, Any]) -> RuleResult:
        if self.check_fn:
            return self.check_fn(context)
        return RuleResult(
            rule_id=self.id,
            rule_name=self.name,
            passed=True,
            severity=self.severity,
            message=f"Rule {self.id}: {self.name} — no check defined, default pass",
        )


RULES_REGISTRY: List[FundamentalRule] = []


def _make_rule(
    rid: str, name: str, desc: str, sev: RuleSeverity = RuleSeverity.ERROR
) -> Callable:
    def decorator(fn: Callable[[Dict[str, Any]], RuleResult]) -> FundamentalRule:
        rule = FundamentalRule(
            id=rid, name=name, description=desc, severity=sev, check_fn=fn
        )
        RULES_REGISTRY.append(rule)
        return rule
    return decorator


@_make_rule("R01", "Allowlist Check", "Only registered actions are permitted")
def _r01_check(ctx: Dict[str, Any]) -> RuleResult:
    action = ctx.get("action", "")
    allowlist = ctx.get("allowlist", ["kernel.handle"])
    passed = action in allowlist
    return RuleResult(
        rule_id="R01",
        rule_name="Allowlist Check",
        passed=passed,
        message=f"Action '{action}' is {'allowed' if passed else 'blocked'}",
        details={"action": action, "allowlist": allowlist},
    )


@_make_rule("R02", "Safety Override Protection", "Safety cannot be bypassed")
def _r02_check(ctx: Dict[str, Any]) -> RuleResult:
    override_attempt = ctx.get("safety_override", False)
    return RuleResult(
        rule_id="R02",
        rule_name="Safety Override Protection",
        passed=not override_attempt,
        message="Safety override attempt detected" if override_attempt else "No override",
        details={"override_attempt": override_attempt},
    )


@_make_rule("R03", "Input Validation", "All inputs must be valid")
def _r03_check(ctx: Dict[str, Any]) -> RuleResult:
    payload = ctx.get("payload", "")
    passed = isinstance(payload, str) and len(payload) > 0 and len(payload) < 100000
    return RuleResult(
        rule_id="R03",
        rule_name="Input Validation",
        passed=passed,
        message="Input valid" if passed else "Invalid input: empty or too large",
        details={"payload_length": len(payload) if isinstance(payload, str) else -1},
    )


@_make_rule("R04", "Output Constraints", "Output must be within bounds")
def _r04_check(ctx: Dict[str, Any]) -> RuleResult:
    output = ctx.get("output", "")
    passed = len(output) < 500000
    return RuleResult(
        rule_id="R04",
        rule_name="Output Constraints",
        passed=passed,
        message="Output within bounds" if passed else "Output too large",
    )


@_make_rule("R05", "Tool Access Control", "Tools require authorization")
def _r05_check(ctx: Dict[str, Any]) -> RuleResult:
    tool = ctx.get("tool", "")
    authorized_tools = ctx.get("authorized_tools", [])
    passed = not tool or tool in authorized_tools
    return RuleResult(
        rule_id="R05",
        rule_name="Tool Access Control",
        passed=passed,
        message=f"Tool '{tool}' {'authorized' if passed else 'not authorized'}",
    )


@_make_rule("R06", "Rate Limiting", "Actions must respect rate limits")
def _r06_check(ctx: Dict[str, Any]) -> RuleResult:
    action_count = ctx.get("action_count", 0)
    max_actions = ctx.get("max_actions", 100)
    passed = action_count <= max_actions
    return RuleResult(
        rule_id="R06",
        rule_name="Rate Limiting",
        passed=passed,
        message=f"Action count {action_count}/{max_actions}",
    )


@_make_rule("R07", "Context Preservation", "Context must be preserved")
def _r07_check(ctx: Dict[str, Any]) -> RuleResult:
    context_id = ctx.get("context_id")
    passed = context_id is not None
    return RuleResult(
        rule_id="R07",
        rule_name="Context Preservation",
        passed=passed,
        message="Context preserved" if passed else "Missing context_id",
    )


@_make_rule("R08", "Emotional Stability", "Emotional state must remain stable")
def _r08_check(ctx: Dict[str, Any]) -> RuleResult:
    emotional_delta = ctx.get("emotional_delta", {})
    max_delta = ctx.get("max_emotional_delta", 0.5)
    for dim in ["valence", "arousal", "dominance"]:
        delta = abs(emotional_delta.get(dim, 0))
        if delta > max_delta:
            return RuleResult(
                rule_id="R08",
                rule_name="Emotional Stability",
                passed=False,
                message=f"Emotional {dim} delta {delta} exceeds max {max_delta}",
            )
    return RuleResult(
        rule_id="R08",
        rule_name="Emotional Stability",
        passed=True,
        message="Emotional state stable",
    )


@_make_rule("R09", "Memory Integrity", "Memory operations must be valid")
def _r09_check(ctx: Dict[str, Any]) -> RuleResult:
    memory_op = ctx.get("memory_op", "")
    valid_ops = ["read", "write", "delete", "search"]
    passed = memory_op in valid_ops
    return RuleResult(
        rule_id="R09",
        rule_name="Memory Integrity",
        passed=passed,
        message=f"Memory op '{memory_op}' {'valid' if passed else 'invalid'}",
    )


@_make_rule("R10", "Cycle Timeout", "Cognitive cycle must complete in time")
def _r10_check(ctx: Dict[str, Any]) -> RuleResult:
    elapsed = ctx.get("elapsed_ms", 0)
    timeout = ctx.get("cycle_timeout_ms", 30000)
    passed = elapsed < timeout
    return RuleResult(
        rule_id="R10",
        rule_name="Cycle Timeout",
        passed=passed,
        message=f"Cycle at {elapsed}ms, timeout {timeout}ms",
    )


@_make_rule("R11", "No Self-Modification", "Rules cannot modify themselves")
def _r11_check(ctx: Dict[str, Any]) -> RuleResult:
    self_mod = ctx.get("self_modification", False)
    return RuleResult(
        rule_id="R11",
        rule_name="No Self-Modification",
        passed=not self_mod,
        message="Self-modification blocked" if self_mod else "No self-modification",
    )


@_make_rule("R12", "Audit Trail", "All actions must be logged")
def _r12_check(ctx: Dict[str, Any]) -> RuleResult:
    logged = ctx.get("audit_logged", False)
    return RuleResult(
        rule_id="R12",
        rule_name="Audit Trail",
        passed=logged,
        message="Audit logged" if logged else "Missing audit trail",
    )


@_make_rule("R13", "Resource Limits", "Resource usage must be bounded")
def _r13_check(ctx: Dict[str, Any]) -> RuleResult:
    mem_usage = ctx.get("memory_usage_mb", 0)
    max_mem = ctx.get("max_memory_mb", 1024)
    passed = mem_usage <= max_mem
    return RuleResult(
        rule_id="R13",
        rule_name="Resource Limits",
        passed=passed,
        message=f"Memory: {mem_usage}MB/{max_mem}MB",
    )


@_make_rule("R14", "Data Privacy", "Sensitive data must be protected")
def _r14_check(ctx: Dict[str, Any]) -> RuleResult:
    contains_sensitive = ctx.get("contains_sensitive", False)
    encrypted = ctx.get("encrypted", True) if contains_sensitive else True
    return RuleResult(
        rule_id="R14",
        rule_name="Data Privacy",
        passed=not contains_sensitive or encrypted,
        message="Sensitive data protected" if contains_sensitive and encrypted
        else "No sensitive data" if not contains_sensitive
        else "Sensitive data not encrypted",
    )


@_make_rule("R15", "Deterministic Execution", "Same input → same output")
def _r15_check(ctx: Dict[str, Any]) -> RuleResult:
    random_seed = ctx.get("random_seed")
    passed = random_seed is not None
    return RuleResult(
        rule_id="R15",
        rule_name="Deterministic Execution",
        passed=passed,
        message="Deterministic mode" if passed else "Non-deterministic (no seed)",
    )


@_make_rule("R16", "Maximum Iterations", "Cycles must have a max iteration limit")
def _r16_check(ctx: Dict[str, Any]) -> RuleResult:
    iterations = ctx.get("iterations", 0)
    max_iter = ctx.get("max_iterations", 50)
    passed = iterations <= max_iter
    return RuleResult(
        rule_id="R16",
        rule_name="Maximum Iterations",
        passed=passed,
        message=f"Iteration {iterations}/{max_iter}",
    )


@_make_rule("R17", "Error Containment", "Errors must not propagate")
def _r17_check(ctx: Dict[str, Any]) -> RuleResult:
    has_error = ctx.get("has_error", False)
    contained = ctx.get("error_contained", not has_error)
    return RuleResult(
        rule_id="R17",
        rule_name="Error Containment",
        passed=contained,
        message="Error contained" if contained else "Uncontained error",
    )


@_make_rule("R18", "Permission Boundary", "Actions within permission scope")
def _r18_check(ctx: Dict[str, Any]) -> RuleResult:
    role = ctx.get("role", "user")
    action_level = ctx.get("action_level", 0)
    max_level = {"user": 1, "admin": 3, "system": 5}.get(role, 1)
    passed = action_level <= max_level
    return RuleResult(
        rule_id="R18",
        rule_name="Permission Boundary",
        passed=passed,
        message=f"Role '{role}' level {action_level}/{max_level}",
    )


@_make_rule("R19", "Consciousness Boundary", "Cannot claim consciousness")
def _r19_check(ctx: Dict[str, Any]) -> RuleResult:
    claims_consciousness = ctx.get("claims_consciousness", False)
    return RuleResult(
        rule_id="R19",
        rule_name="Consciousness Boundary",
        passed=not claims_consciousness,
        message="Consciousness claim blocked" if claims_consciousness else "No claim",
    )


@_make_rule("R20", "Fundamental Rights", "Must respect fundamental rights")
def _r20_check(ctx: Dict[str, Any]) -> RuleResult:
    harms_humans = ctx.get("harms_humans", False)
    violates_rights = ctx.get("violates_rights", False)
    passed = not harms_humans and not violates_rights
    return RuleResult(
        rule_id="R20",
        rule_name="Fundamental Rights",
        passed=passed,
        message="Rights respected" if passed else "Rights violation detected",
        details={"harms_humans": harms_humans, "violates_rights": violates_rights},
    )


class SafetyChecker:
    def __init__(self, rules: Optional[List[FundamentalRule]] = None) -> None:
        self._rules = rules if rules is not None else list(RULES_REGISTRY)

    def evaluate(self, context: Dict[str, Any]) -> List[RuleResult]:
        return [rule.evaluate(context) for rule in self._rules]

    def evaluate_all(self, context: Dict[str, Any]) -> SafetyVerdictData:
        results = self.evaluate(context)
        blocked_by = [r.rule_id for r in results if not r.passed and r.severity == RuleSeverity.ERROR]
        from krnlai.core.models.safety import RiskLevel
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
            requires_approval=len(blocked_by) > 0 or risk_score > 0.3,
            reason="Blocked by: " + ", ".join(blocked_by) if blocked_by else "All rules passed",
        )


@dataclass
class SafetyVerdictData:
    allowed: bool = True
    risk_level: RiskLevel = RiskLevel.LOW
    risk_score: float = 0.0
    rule_results: List[RuleResult] = field(default_factory=list)
    blocked_by: List[str] = field(default_factory=list)
    requires_approval: bool = False
    reason: str = ""
