from __future__ import annotations

import pytest

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.steps.attention import EnhancedAttentionStep


class TestEnhancedAttentionStep:
    @pytest.mark.asyncio
    async def test_Execute_Question_ShouldClassifyIntent(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="What is the capital of France?")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "question"

    @pytest.mark.asyncio
    async def test_Execute_Command_ShouldClassifyIntent(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="run the analysis script")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "command"

    @pytest.mark.asyncio
    async def test_Execute_Analysis_ShouldClassifyIntent(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="analyze the sales data for Q3")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "analysis"

    @pytest.mark.asyncio
    async def test_Execute_Creative_ShouldClassifyIntent(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="write a poem about autumn")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "creative"

    @pytest.mark.asyncio
    async def test_Execute_Metacognitive_ShouldClassifyIntent(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="I reflect and ponder on this idea")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "metacognitive"

    @pytest.mark.asyncio
    async def test_Execute_Statement_ShouldBeDefault(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="The sky is blue.")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["intent"] == "statement"

    @pytest.mark.asyncio
    async def test_Execute_ExtractTopic(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="Tell me about machine learning algorithms")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert isinstance(result["topic"], str)
        assert len(result["topic"]) > 0

    @pytest.mark.asyncio
    async def test_Execute_Complexity_LongText_ShouldBeHigher(self):
        step = EnhancedAttentionStep()
        short_cmd = CommandEnvelope(payload="Hello")
        payload = "If we analyze the algorithm architecture and mathematical foundations, then we can evaluate the system performance under various conditions. " * 5
        long_cmd = CommandEnvelope(payload=payload)
        state = CognitiveState()
        short_result = await step.execute(short_cmd, state, {})
        long_result = await step.execute(long_cmd, state, {})
        assert long_result["complexity"] > short_result["complexity"]

    @pytest.mark.asyncio
    async def test_Execute_Urgency_Detected(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="This is urgent, need it ASAP!")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["urgency"] > 0.5

    @pytest.mark.asyncio
    async def test_Execute_Urgency_None_ShouldBeLow(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="What is the weather like?")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["urgency"] == 0.0

    @pytest.mark.asyncio
    async def test_Execute_ThoughtType_Analytical(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="Compare and evaluate the two approaches")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["thought_type"] == "analytical"

    @pytest.mark.asyncio
    async def test_Execute_ThoughtType_Procedural(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="How to install the package step by step")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["thought_type"] == "procedural"

    @pytest.mark.asyncio
    async def test_Execute_Category_MapsCorrectly(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload="Recall the previous conversation from earlier")
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["category"] == "episodic_recall"

    @pytest.mark.asyncio
    async def test_Execute_RequiresDecomposition_HighComplexity(self):
        step = EnhancedAttentionStep()
        cmd = CommandEnvelope(payload=("If " * 50) + ("algorithm " * 50) + ("architecture " * 50))
        state = CognitiveState()
        result = await step.execute(cmd, state, {})
        assert result["requires_decomposition"] == (result["complexity"] > 0.7)

    @pytest.mark.asyncio
    async def test_ClassifyIntent_EmptyText_ShouldReturnStatement(self):
        step = EnhancedAttentionStep()
        assert step._classify_intent("") == "statement"
