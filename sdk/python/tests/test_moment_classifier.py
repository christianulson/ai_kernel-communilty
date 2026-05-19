from __future__ import annotations

import pytest

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.models.moment import MomentCategory, MomentImportance, MomentNarrativeRole
from krnlai.core.steps.moment_classifier import MomentClassifierStep, MomentSnapshot
from krnlai.core.stores.moment_store import MomentStore


class TestMomentCategory:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert MomentCategory.ROUTINE.value == "routine"
        assert MomentCategory.LEARNING.value == "learning"
        assert MomentCategory.ANOMALY.value == "anomaly"
        assert MomentCategory.CONFLICT.value == "conflict"

    def test_Enum_Count_ShouldHave4Values(self):
        assert len(MomentCategory) == 4


class TestMomentImportance:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert MomentImportance.ZERO.value == 0
        assert MomentImportance.LOW.value == 1
        assert MomentImportance.MEDIUM.value == 2
        assert MomentImportance.HIGH.value == 3
        assert MomentImportance.CRITICAL.value == 4

    def test_Enum_Count_ShouldHave5Values(self):
        assert len(MomentImportance) == 5


class TestMomentNarrativeRole:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert MomentNarrativeRole.NONE.value == "none"
        assert MomentNarrativeRole.SETUP.value == "setup"
        assert MomentNarrativeRole.TURNING_POINT.value == "turning_point"
        assert MomentNarrativeRole.RESOLUTION.value == "resolution"
        assert MomentNarrativeRole.CLIMAX.value == "climax"

    def test_Enum_Count_ShouldHave5Values(self):
        assert len(MomentNarrativeRole) == 5


class TestMomentClassifierStep:
    @pytest.mark.asyncio
    async def test_Execute_NormalInput_ShouldBeRoutine(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="Hello, how are you?")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {})
        assert result["moment_category"] == MomentCategory.ROUTINE
        assert result["moment_importance"] == MomentImportance.ZERO

    @pytest.mark.asyncio
    async def test_Execute_WithError_ShouldBeAnomaly(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState(errors=["error occurred"])
        result = await classifier.execute(cmd, state, {})
        assert result["moment_category"] == MomentCategory.ANOMALY

    @pytest.mark.asyncio
    async def test_Execute_HighNovelty_ShouldBeLearning(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="Analyze the algorithm complexity and mathematical foundations of the distributed computing system for optimal performance evaluation across multiple dimensions of analysis. " * 3)
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {"novelty": 0.8})
        assert result["moment_category"] == MomentCategory.LEARNING

    @pytest.mark.asyncio
    async def test_Execute_HighRisk_ShouldBeConflict(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {"risk_score": 0.8})
        assert result["moment_category"] == MomentCategory.CONFLICT

    @pytest.mark.asyncio
    async def test_Execute_AllKeys_ShouldBePresent(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {"risk_score": 0.3})
        assert "moment_category" in result
        assert "moment_confidence" in result
        assert "moment_importance" in result
        assert "moment_narrative_role" in result
        assert "moment_cognitive_load" in result
        assert "moment_arousal" in result
        assert "moment_valence" in result
        assert "moment_id" in result

    @pytest.mark.asyncio
    async def test_Execute_Confidence_Routine_ShouldBeHigh(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {})
        assert result["moment_confidence"] >= 0.8

    @pytest.mark.asyncio
    async def test_Execute_CognitiveLoad_ShouldBeCalculated(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {"risk_score": 0.5})
        assert 0.0 <= result["moment_cognitive_load"] <= 1.0

    @pytest.mark.asyncio
    async def test_Execute_NarrativeRole_HighRisk_ShouldBeClimax(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {"risk_score": 0.8})
        assert result["moment_narrative_role"] == MomentNarrativeRole.CLIMAX

    @pytest.mark.asyncio
    async def test_Execute_History_ShouldAccumulate(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        await classifier.execute(cmd, state, {})
        await classifier.execute(cmd, state, {})
        assert len(classifier.moment_history) == 2

    @pytest.mark.asyncio
    async def test_Execute_EmptyInput_ShouldBeRoutine(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="")
        state = CognitiveState()
        result = await classifier.execute(cmd, state, {})
        assert result["moment_category"] == MomentCategory.ROUTINE

    @pytest.mark.asyncio
    async def test_Execute_ErrorWithHighNovelty_ErrorShouldWin(self):
        classifier = MomentClassifierStep()
        cmd = CommandEnvelope(payload="Analyze the algorithm complexity and mathematical foundations of the distributed computing system for optimal performance evaluation across multiple dimensions of analysis. " * 3)
        state = CognitiveState(errors=["critical failure"])
        result = await classifier.execute(cmd, state, {"novelty": 0.9})
        assert result["moment_category"] == MomentCategory.ANOMALY

    @pytest.mark.asyncio
    async def test_MomentSnapshot_Creation_ShouldSucceed(self):
        snap = MomentSnapshot(
            category=MomentCategory.LEARNING,
            confidence=0.85,
            importance=MomentImportance.HIGH,
        )
        assert snap.category == MomentCategory.LEARNING
        assert snap.confidence == 0.85
        assert snap.importance == MomentImportance.HIGH
        assert snap.moment_id is not None


class TestMomentStore:
    def test_AddAndListRecent_ShouldReturnOrdered(self):
        store = MomentStore()
        snap1 = MomentSnapshot(category=MomentCategory.ROUTINE)
        snap2 = MomentSnapshot(category=MomentCategory.LEARNING)
        store.add(snap1.moment_id, snap1)
        store.add(snap2.moment_id, snap2)
        recent = store.list_recent(2)
        assert len(recent) == 2

    def test_FilterByCategory_ShouldMatch(self):
        store = MomentStore()
        routine = MomentSnapshot(category=MomentCategory.ROUTINE)
        anomaly = MomentSnapshot(category=MomentCategory.ANOMALY)
        store.add(routine.moment_id, routine)
        store.add(anomaly.moment_id, anomaly)
        filtered = store.filter_by_category(MomentCategory.ANOMALY)
        assert len(filtered) == 1
        assert filtered[0].category == MomentCategory.ANOMALY

    def test_FilterByCategory_NoMatch_ShouldBeEmpty(self):
        store = MomentStore()
        snap = MomentSnapshot(category=MomentCategory.ROUTINE)
        store.add(snap.moment_id, snap)
        filtered = store.filter_by_category(MomentCategory.CONFLICT)
        assert len(filtered) == 0

    def test_ListRecent_Limit_ShouldRespect(self):
        store = MomentStore()
        for _ in range(5):
            snap = MomentSnapshot(category=MomentCategory.LEARNING)
            store.add(snap.moment_id, snap)
        recent = store.list_recent(3)
        assert len(recent) == 3


class TestCognitiveCycleIntegration:
    @pytest.mark.asyncio
    async def test_Runner_ShouldStoreMomentSnapshot(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        runner = CognitiveCycleRunner()
        await runner.run("test")
        assert runner.moment_store.count() > 0

    @pytest.mark.asyncio
    async def test_Runner_MomentData_ShouldFlowToMetacognition(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        runner = CognitiveCycleRunner()
        await runner.run("analyze this data carefully")
        stored = runner.moment_store.all()
        assert len(stored) > 0
        assert stored[0].category is not None
