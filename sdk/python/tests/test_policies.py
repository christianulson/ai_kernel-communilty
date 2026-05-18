from __future__ import annotations

from krnlai.core.policies.engine import PolicyEngine, PolicyRule


class TestPolicyEngine:
    def test_Evaluate_NoRules_ShouldBeEmpty(self):
        engine = PolicyEngine()
        result = engine.evaluate({})
        assert len(result) == 0

    def test_AddRule_WithCondition_ShouldTrigger(self):
        engine = PolicyEngine()
        rule = PolicyRule(
            id="test_rule",
            name="Test Rule",
            condition=lambda ctx: ctx.get("value") == 42,
            action=lambda ctx: {**ctx, "processed": True},
            priority=10,
        )
        engine.add_rule(rule)
        triggered = engine.evaluate({"value": 42})
        assert len(triggered) == 1

    def test_Execute_ShouldReturnUpdatedContext(self):
        engine = PolicyEngine()
        engine.add_rule(PolicyRule(
            id="add_flag",
            name="Add Flag",
            condition=lambda ctx: True,
            action=lambda ctx: {**ctx, "flagged": True},
            priority=0,
        ))
        result = engine.execute({"key": "value"})
        assert result["flagged"] is True

    def test_DisableRule_ShouldNotTrigger(self):
        engine = PolicyEngine()
        engine.add_rule(PolicyRule(
            id="always",
            name="Always",
            condition=lambda ctx: True,
            action=lambda ctx: {**ctx, "triggered": True},
            priority=0,
        ))
        engine.disable_rule("always")
        triggered = engine.evaluate({"test": True})
        assert len(triggered) == 0

    def test_EnableRule_AfterDisable_ShouldWork(self):
        engine = PolicyEngine()
        engine.add_rule(PolicyRule(
            id="toggle",
            name="Toggle Rule",
            condition=lambda ctx: True,
            action=lambda ctx: ctx,
            priority=0,
        ))
        engine.disable_rule("toggle")
        engine.enable_rule("toggle")
        assert len(engine.evaluate({})) == 1

    def test_Priority_ShouldOrderRules(self):
        engine = PolicyEngine()
        engine.add_rule(PolicyRule(
            id="low",
            name="Low Priority",
            condition=lambda ctx: True,
            priority=0,
        ))
        engine.add_rule(PolicyRule(
            id="high",
            name="High Priority",
            condition=lambda ctx: True,
            priority=100,
        ))
        rules = engine.rules
        assert rules[0].id == "high"

    def test_NoCondition_ShouldNotMatch(self):
        engine = PolicyEngine()
        engine.add_rule(PolicyRule(
            id="no_condition",
            name="No Condition",
            priority=0,
        ))
        assert len(engine.evaluate({})) == 0
