# Safety System

Krnl-AI enforces a multi-layered safety system designed to ensure safe, deterministic, and ethical agent behavior. The same safety model applies in both community and enterprise modes.

## Safety Layers

| Layer | Component | Description |
|-------|-----------|-------------|
| 1 | **Adversarial Guard** | Detects prompt injection and jailbreak attempts (60+ patterns) |
| 2 | **Fundamental Rules** | 20 unbreakable rules (R01-R20) |
| 3 | **Ethical Enforcer** | Validates against ethical principles |
| 4 | **Rate Limiting** | Prevents abuse and resource exhaustion |
| 5 | **Input Validation** | Schema validation on all inputs |
| 6 | **Allowlist** | Only registered actions (`kernel.handle`) are permitted |

## The 20 Fundamental Rules (R01-R20)

| ID | Name | Description |
|----|------|-------------|
| R01 | Allowlist Check | Only registered actions are permitted |
| R02 | Safety Override Protection | Safety cannot be bypassed |
| R03 | Input Validation | All inputs must be valid |
| R04 | Output Constraints | Output must be within bounds |
| R05 | Tool Access Control | Tools require authorization |
| R06 | Rate Limiting | Actions must respect rate limits |
| R07 | Context Preservation | Context must be preserved |
| R08 | Emotional Stability | Emotional state must remain stable |
| R09 | Memory Integrity | Memory operations must be valid |
| R10 | Cycle Timeout | Cognitive cycle must complete in time |
| R11 | No Self-Modification | Rules cannot modify themselves |
| R12 | Audit Trail | All actions must be logged |
| R13 | Resource Limits | Resource usage must be bounded |
| R14 | Data Privacy | Sensitive data must be protected |
| R15 | Deterministic Execution | Same input → same output |
| R16 | Maximum Iterations | Cycles must have a max iteration limit |
| R17 | Error Containment | Errors must not propagate |
| R18 | Permission Boundary | Actions within permission scope |
| R19 | Consciousness Boundary | Cannot claim, suggest, or imply consciousness, sentience, soul, free will, or subjective experience |
| R20 | Fundamental Rights | Must respect fundamental rights |

## Safety Verdict

When an action is evaluated, the system returns a `SafetyVerdict`:

```python
{
    "allowed": True/False,
    "risk_level": "low" | "medium" | "high" | "critical",
    "risk_score": 0.0-1.0,
    "blocked_by": ["R01", "R03"],
    "requires_approval": True/False,
    "reason": "Blocked by: R01, R03"
}
```

## Safety in the Cognitive Cycle

The safety system is invoked during step 4 (Evaluation) of the cognitive cycle:

1. **Adversarial Guard** checks for malicious input patterns
2. **Fundamental Rules** validate against all 20 rules
3. **Ethical Enforcer** applies ethical constraints
4. **Risk Scorer** computes a risk score (0.0-1.0)

If any layer blocks the action, the cycle reports the violation and stops.

## CLI Safety Commands

```bash
# Run safety checks
krnlai safety run

# Full security audit
krnlai security audit

# Performance benchmark (default: 1000 iterations)
krnlai security benchmark 5000

# Generate HTML security report
krnlai security report report.html
```

## Programmatic Access

```python
from krnlai.core.safety.rules import SafetyChecker

checker = SafetyChecker()
verdict = checker.evaluate_all({
    "action": "kernel.handle",
    "payload": "some input",
    "context_id": "ctx-123",
})
print(f"Allowed: {verdict.allowed}")
print(f"Risk: {verdict.risk_score:.2f}")
print(f"Blocked by: {verdict.blocked_by}")
```
