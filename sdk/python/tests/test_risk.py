from __future__ import annotations

from krnlai.core.risk.scorer import RiskScorer


class TestRiskScorer:
    def test_Evaluate_EmptyContext_ShouldBeZero(self):
        scorer = RiskScorer()
        score = scorer.evaluate({})
        assert score == 0.0

    def test_Evaluate_LongInput_ShouldIncrease(self):
        scorer = RiskScorer()
        score = scorer.evaluate({"payload": "x" * 6000})
        assert score > 0.0

    def test_Evaluate_ToolUse_ShouldIncrease(self):
        scorer = RiskScorer()
        score = scorer.evaluate({"payload": "hello", "tool": "calculator"})
        assert score > 0.0

    def test_Evaluate_HighFrequency_ShouldIncrease(self):
        scorer = RiskScorer()
        score = scorer.evaluate({"payload": "hello", "action_count": 100})
        assert score > 0.0

    def test_Evaluate_NegativeEmotion_ShouldIncrease(self):
        scorer = RiskScorer()
        score = scorer.evaluate({"payload": "hello", "emotional_valence": -0.8})
        assert score > 0.0

    def test_Score_Combined_ShouldBeMaxOne(self):
        scorer = RiskScorer()
        score = scorer.evaluate({
            "payload": "x" * 6000,
            "tool": "some_tool",
            "action_count": 100,
            "emotional_valence": -0.9,
            "history_risk": 0.8,
        })
        assert score <= 1.0

    def test_Reset_ShouldClearFactors(self):
        scorer = RiskScorer()
        scorer.evaluate({"payload": "x" * 6000})
        assert len(scorer.factors) > 0
        scorer.reset()
        assert len(scorer.factors) == 0

    def test_Factors_ShouldProvideReasons(self):
        scorer = RiskScorer()
        scorer.evaluate({"payload": "x" * 6000, "tool": "calc"})
        reasons = [f.reason for f in scorer.factors]
        assert any("Input length" in r for r in reasons)
        assert any("Tool execution" in r for r in reasons)
