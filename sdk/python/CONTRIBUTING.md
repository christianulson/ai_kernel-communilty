# Contributing

## TDD Requirement

All contributions must follow Test-Driven Development:

1. **RED** — Write the test first, verify it fails
2. **GREEN** — Write minimum code to pass the test
3. **REFACTOR** — Improve code while keeping tests green

## Setup

```bash
pip install -e ".[all,dev]"
pip install pytest pytest-asyncio ruff mypy
```

## Running Tests

```bash
pytest tests/ -v
pytest tests/test_safety.py -v  # specific module
```

## Code Quality

```bash
ruff check .
mypy krnlai/
```

## Naming Convention

```
test_{Class}_{Scenario}_{ExpectedResult}
```

Example:
```python
async def test_SafetyChecker_HighRiskPlan_ShouldRequireApproval():
    ...
```

## PR Checklist

- [ ] Tests written before code
- [ ] All tests pass
- [ ] Ruff passes
- [ ] MyPy passes
- [ ] No external dependencies added
