from __future__ import annotations

from uuid import UUID

import pytest
from pydantic import ValidationError

from krnlai.core.cycle import CognitiveCycleRunner
from krnlai.core.models.cognitive import CycleEvent, CycleStep
from krnlai.core.models.thought import (
    ThoughtCategory,
    ThoughtClassification,
    ThoughtHorizon,
    ThoughtTrigger,
)


class TestThoughtCategory:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert ThoughtCategory.SENSORY_INPUT.value == "sensory_input"
        assert ThoughtCategory.EPISODIC_RECALL.value == "episodic_recall"
        assert ThoughtCategory.DEDUCTIVE.value == "deductive"
        assert ThoughtCategory.EVALUATIVE.value == "evaluative"
        assert ThoughtCategory.GOAL_DECOMPOSITION.value == "goal_decomposition"
        assert ThoughtCategory.SELF_OBSERVATION.value == "self_observation"
        assert ThoughtCategory.ANALOGY_FORMATION.value == "analogy_formation"
        assert ThoughtCategory.THEORY_OF_MIND.value == "theory_of_mind"

    def test_Enum_Count_ShouldHave30Values(self):
        assert len(ThoughtCategory) == 30

    def test_Enum_Groups_ShouldBeCorrect(self):
        values = list(ThoughtCategory)
        assert values[0:3] == [
            ThoughtCategory.SENSORY_INPUT,
            ThoughtCategory.PATTERN_MATCH,
            ThoughtCategory.CONTEXT_AWARE,
        ]
        assert values[3:8] == [
            ThoughtCategory.EPISODIC_RECALL,
            ThoughtCategory.SEMANTIC_RECALL,
            ThoughtCategory.WORKING_MEMORY,
            ThoughtCategory.EMOTIONAL_RECALL,
            ThoughtCategory.PROCEDURAL_RECALL,
        ]
        assert values[8:14] == [
            ThoughtCategory.DEDUCTIVE,
            ThoughtCategory.INDUCTIVE,
            ThoughtCategory.ABDUCTIVE,
            ThoughtCategory.ANALOGICAL,
            ThoughtCategory.CAUSAL,
            ThoughtCategory.COUNTERFACTUAL,
        ]
        assert values[14:18] == [
            ThoughtCategory.EVALUATIVE,
            ThoughtCategory.COMPARATIVE,
            ThoughtCategory.RISK_ASSESSMENT,
            ThoughtCategory.POLICY_CHECK,
        ]
        assert values[18:21] == [
            ThoughtCategory.GOAL_DECOMPOSITION,
            ThoughtCategory.SEQUENCE_PLANNING,
            ThoughtCategory.CONTINGENCY_PLANNING,
        ]
        assert values[21:25] == [
            ThoughtCategory.SELF_OBSERVATION,
            ThoughtCategory.CONFIDENCE_ESTIMATION,
            ThoughtCategory.BIAS_DETECTION,
            ThoughtCategory.REASONING_QUALITY,
        ]
        assert values[25:28] == [
            ThoughtCategory.ANALOGY_FORMATION,
            ThoughtCategory.HYPOTHESIS_GENERATION,
            ThoughtCategory.INSIGHT,
        ]
        assert values[28:30] == [
            ThoughtCategory.THEORY_OF_MIND,
            ThoughtCategory.COMMUNICATION,
        ]


class TestThoughtHorizon:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert ThoughtHorizon.PAST.value == "past"
        assert ThoughtHorizon.PRESENT.value == "present"
        assert ThoughtHorizon.NEAR_FUTURE.value == "near_future"
        assert ThoughtHorizon.FAR_FUTURE.value == "far_future"

    def test_Enum_Count_ShouldHave4Values(self):
        assert len(ThoughtHorizon) == 4


class TestThoughtTrigger:
    def test_Enum_AllMembers_ShouldBeAccessible(self):
        assert ThoughtTrigger.INTERNAL.value == "internal"
        assert ThoughtTrigger.EXTERNAL.value == "external"
        assert ThoughtTrigger.MEMORY.value == "memory"
        assert ThoughtTrigger.INFERENCE.value == "inference"

    def test_Enum_Count_ShouldHave4Values(self):
        assert len(ThoughtTrigger) == 4


class TestThoughtClassification:
    def test_Create_AllFields_ShouldSucceed(self):
        tc = ThoughtClassification(
            category=ThoughtCategory.DEDUCTIVE,
            subcategory="modus_ponens",
            complexity=0.8,
            abstractness=0.6,
            novelty=0.3,
            confidence=0.9,
            horizon=ThoughtHorizon.PRESENT,
            duration_ms=150.0,
            valence_delta=0.1,
            arousal_delta=0.2,
            trigger=ThoughtTrigger.INFERENCE,
            antecedents=[UUID(int=1), UUID(int=2)],
        )
        assert tc.category == ThoughtCategory.DEDUCTIVE
        assert tc.subcategory == "modus_ponens"
        assert tc.complexity == 0.8
        assert tc.abstractness == 0.6
        assert tc.novelty == 0.3
        assert tc.confidence == 0.9
        assert tc.horizon == ThoughtHorizon.PRESENT
        assert tc.duration_ms == 150.0
        assert tc.valence_delta == 0.1
        assert tc.arousal_delta == 0.2
        assert tc.trigger == ThoughtTrigger.INFERENCE
        assert len(tc.antecedents) == 2
        assert isinstance(tc.thought_id, UUID)

    def test_Create_DefaultValues_ShouldBeSensible(self):
        tc = ThoughtClassification(category=ThoughtCategory.SENSORY_INPUT)
        assert tc.complexity == 0.5
        assert tc.abstractness == 0.5
        assert tc.novelty == 0.5
        assert tc.confidence == 0.5
        assert tc.horizon == ThoughtHorizon.PRESENT
        assert tc.trigger == ThoughtTrigger.INTERNAL
        assert tc.antecedents == []
        assert tc.subcategory is None
        assert tc.duration_ms == 0.0
        assert tc.valence_delta == 0.0
        assert tc.arousal_delta == 0.0

    def test_Serialize_Roundtrip_ShouldMatch(self):
        tc = ThoughtClassification(
            category=ThoughtCategory.CAUSAL,
            complexity=0.7,
            novelty=0.4,
        )
        json_str = tc.model_dump_json()
        restored = ThoughtClassification.model_validate_json(json_str)
        assert restored.category == tc.category
        assert restored.complexity == tc.complexity
        assert restored.novelty == tc.novelty
        assert restored.thought_id == tc.thought_id

    def test_Complexity_OutOfRange_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, complexity=1.5)

    def test_Complexity_Negative_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, complexity=-0.1)

    def test_Abstractness_OutOfRange_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, abstractness=1.5)

    def test_Abstractness_Negative_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, abstractness=-0.1)

    def test_Novelty_OutOfRange_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, novelty=1.5)

    def test_Novelty_Negative_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, novelty=-0.1)

    def test_Confidence_OutOfRange_ShouldRaise(self):
        with pytest.raises(ValidationError):
            ThoughtClassification(category=ThoughtCategory.INSIGHT, confidence=-0.1)


class TestCycleEventIntegration:
    @pytest.mark.asyncio
    async def test_CycleEvent_Classification_ShouldBeNoneByDefault(self):
        event = CycleEvent(cycle_id=UUID(int=0), step=CycleStep.SENSOR)
        assert event.classification is None

    @pytest.mark.asyncio
    async def test_CycleEvent_WithClassification_ShouldAttach(self):
        tc = ThoughtClassification(category=ThoughtCategory.DEDUCTIVE)
        event = CycleEvent(
            cycle_id=UUID(int=0),
            step=CycleStep.EVALUATION,
            classification=tc,
        )
        assert event.classification is not None
        assert event.classification.category == ThoughtCategory.DEDUCTIVE

    @pytest.mark.asyncio
    async def test_CognitiveCycleRunner_EachStep_HasClassification(self):
        runner = CognitiveCycleRunner()
        events = []

        def capture(event):
            events.append(event)

        runner.on_event(capture)
        await runner.run("test classification")
        assert len(events) > 0
        for event in events:
            assert event.classification is not None
            assert isinstance(event.classification, ThoughtClassification)
            assert event.classification.complexity >= 0.0
            assert event.classification.complexity <= 1.0
            assert event.classification.confidence >= 0.0
            assert event.classification.confidence <= 1.0

    @pytest.mark.asyncio
    async def test_SensorStep_ShouldBeSensoryInput(self):
        runner = CognitiveCycleRunner()
        events = []

        def capture(event):
            events.append(event)

        runner.on_event(capture)
        await runner.run("test")
        sensor_event = events[0]
        assert sensor_event.step == CycleStep.SENSOR
        assert sensor_event.classification is not None
        assert sensor_event.classification.category == ThoughtCategory.SENSORY_INPUT
        assert sensor_event.classification.trigger == ThoughtTrigger.EXTERNAL

    @pytest.mark.asyncio
    async def test_MetacognitionStep_ShouldBeSelfObservation(self):
        runner = CognitiveCycleRunner()
        events = []

        def capture(event):
            events.append(event)

        runner.on_event(capture)
        await runner.run("test")
        meta_events = [e for e in events if e.step == CycleStep.METACOGNITION]
        assert len(meta_events) > 0
        assert meta_events[0].classification is not None
        assert meta_events[0].classification.category == ThoughtCategory.SELF_OBSERVATION
