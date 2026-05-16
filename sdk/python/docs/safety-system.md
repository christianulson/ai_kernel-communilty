# Safety System

The AI Kernel enforces 20 fundamental rules (R01-R20) that cannot be overridden.

## Rules Overview

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
| R19 | Consciousness Boundary | Cannot claim consciousness |
| R20 | Fundamental Rights | Must respect fundamental rights |

## CLI Audit

```bash
aikernel security audit
aikernel security benchmark
aikernel security report report.html
```
