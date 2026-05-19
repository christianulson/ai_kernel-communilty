from __future__ import annotations

import pytest

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.steps.metacognition import EnhancedMetacognitionStep


class TestEnhancedMetacognitionStep:
    @pytest.mark.asyncio
    async def test_Execute_SimpleInput_ShouldReturnResult(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="What is the weather?")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert "reasoning_quality" in result
        assert "reasoning_issues" in result
        assert "biases_detected" in result
        assert "calibrated_confidence" in result
        assert "cognitive_load" in result
        assert "decision_type" in result
        assert "observations" in result
        assert isinstance(result["observations"], list)

    @pytest.mark.asyncio
    async def test_Execute_RichInput_ShouldHaveBetterQuality(self):
        step = EnhancedMetacognitionStep()
        payload = "Because the data shows a clear trend, therefore we can conclude that the hypothesis is correct. Furthermore, additional studies support this finding."
        cmd = CommandEnvelope(payload=payload)
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["reasoning_quality"] >= 0.3

    @pytest.mark.asyncio
    async def test_Execute_BiasedText_ShouldDetectBiases(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="I knew it all along, this definitely proves my theory. Everyone agrees with me.")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert len(result["biases_detected"]) > 0

    @pytest.mark.asyncio
    async def test_Execute_ShouldContainReasoningDetails(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="Because of X, therefore Y is true.")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert isinstance(result["reasoning_strengths"], list)
        assert isinstance(result["reasoning_coherence"], float)
        assert isinstance(result["reasoning_completeness"], float)
        assert 0.0 <= result["reasoning_coherence"] <= 1.0
        assert 0.0 <= result["reasoning_completeness"] <= 1.0

    @pytest.mark.asyncio
    async def test_Execute_CalibratedConfidence_ShouldBeInRange(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await step.execute(cmd, state, {"confidence": 0.9})
        assert 0.0 <= result["calibrated_confidence"] <= 1.0
        assert isinstance(result["calibration_adjustment"], float)
        assert isinstance(result["calibration_error"], float)
        assert isinstance(result["calibration_reason"], str)

    @pytest.mark.asyncio
    async def test_Execute_CognitiveLoad_ShouldBeMeasured(self):
        step = EnhancedMetacognitionStep()
        payload = "Analyze the algorithm complexity and mathematical foundations of the system architecture."
        cmd = CommandEnvelope(payload=payload)
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert 0.0 <= result["cognitive_load"] <= 1.0
        assert 0.0 <= result["cognitive_load_intrinsic"] <= 1.0
        assert 0.0 <= result["cognitive_load_extraneous"] <= 1.0
        assert 0.0 <= result["cognitive_load_germane"] <= 1.0

    @pytest.mark.asyncio
    async def test_Execute_DecisionType_HighRisk(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await step.execute(cmd, state, {"risk_score": 0.8})
        assert result["decision_type"] == "high_risk"

    @pytest.mark.asyncio
    async def test_Execute_DecisionType_Urgent(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await step.execute(cmd, state, {"urgency": 0.8})
        assert result["decision_type"] == "time_sensitive"

    @pytest.mark.asyncio
    async def test_Execute_DecisionType_Routine(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["decision_type"] == "routine"

    @pytest.mark.asyncio
    async def test_Execute_RequiresIntervention_ManyBiases(self):
        step = EnhancedMetacognitionStep()
        biased_text = "I knew it all along, this definitely proves my theory. " \
                      "Everyone agrees with me. I feel this is obviously the truth. " \
                      "Based on my first impression, this is certainly correct. " \
                      "Recent examples all show the same thing."
        cmd = CommandEnvelope(payload=biased_text)
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        if len(result["biases_detected"]) > 2:
            assert result["requires_intervention"] is True

    @pytest.mark.asyncio
    async def test_Execute_RequiresDecomposition_HighLoad(self):
        step = EnhancedMetacognitionStep()
        cmd = CommandEnvelope(payload="Analyze the algorithm complexity and mathematical foundations " \
                                      "of the system architecture for the distributed computing framework. " \
                                      "Evaluate the performance characteristics under various load conditions.")
        state = CognitiveState()
        result = await step.execute(cmd, state, {"novelty": 0.8})
        if result["cognitive_load"] > 0.7:
            assert result["requires_decomposition"] is True
