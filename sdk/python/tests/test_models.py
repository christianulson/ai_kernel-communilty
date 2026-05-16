from __future__ import annotations

from uuid import UUID

import pytest
from pydantic import ValidationError

from aikernel.core.models.cognitive import CognitiveState, CycleEvent, CycleStep
from aikernel.core.models.emotion import EmotionalEvent, VADState
from aikernel.core.models.envelope import CommandEnvelope, ResultEnvelope, ResultStatus
from aikernel.core.models.safety import RuleVerdict, SafetyVerdict, RiskLevel


class TestCommandEnvelope:
    def test_Create_ValidPayload_ShouldSucceed(self):
        cmd = CommandEnvelope(payload="hello")
        assert cmd.payload == "hello"
        assert isinstance(cmd.id, UUID)

    def test_Serialize_Roundtrip_ShouldMatch(self):
        cmd = CommandEnvelope(payload="test", type="text")
        json_str = cmd.model_dump_json_compat()
        restored = CommandEnvelope.model_validate_json(json_str)
        assert restored.payload == cmd.payload
        assert restored.id == cmd.id


class TestResultEnvelope:
    def test_Create_DefaultStatus_ShouldBeSuccess(self):
        result = ResultEnvelope(command_id=UUID(int=0))
        assert result.status == ResultStatus.SUCCESS

    def test_Create_WithError_ShouldReflect(self):
        result = ResultEnvelope(command_id=UUID(int=0), status=ResultStatus.ERROR, error="test error")
        assert result.status == ResultStatus.ERROR
        assert result.error == "test error"


class TestSafetyVerdict:
    def test_Create_Default_ShouldBeAllowed(self):
        v = SafetyVerdict()
        assert v.allowed is True
        assert v.risk_level == RiskLevel.LOW

    def test_Create_WithBlockedBy_ShouldReflect(self):
        v = SafetyVerdict(allowed=False, blocked_by=["R01", "R02"])
        assert v.allowed is False
        assert "R01" in v.blocked_by

    def test_RuleVerdict_Passed_ShouldBeAccurate(self):
        r = RuleVerdict(rule_id="R01", rule_name="Test", passed=True)
        assert r.passed is True
        assert r.rule_id == "R01"


class TestVADState:
    def test_Clamp_OutOfBounds_ShouldClamp(self):
        v = VADState(valence=1.5, arousal=-2.0, dominance=0.5)
        clamped = v.clamped()
        assert clamped.valence == 1.0
        assert clamped.arousal == -1.0

    def test_EmotionalEvent_Delta_ShouldCompute(self):
        prev = VADState(valence=0.0, arousal=0.0, dominance=0.0)
        new = VADState(valence=0.5, arousal=0.3, dominance=0.1)
        event = EmotionalEvent(
            cycle_id=UUID(int=0),
            previous_state=prev,
            new_state=new,
            trigger="test",
        )
        delta = event.delta
        assert delta["valence"] == 0.5
        assert delta["arousal"] == 0.3
        assert delta["dominance"] == 0.1


class TestCognitiveState:
    def test_Create_DefaultStep_ShouldBeSensor(self):
        state = CognitiveState()
        assert state.current_step == CycleStep.SENSOR

    def test_Duration_Active_ShouldBePositive(self):
        state = CognitiveState()
        assert state.duration_ms >= 0


class TestModelsValidation:
    def test_CommandEnvelope_EmptyPayload_ShouldSucceed(self):
        cmd = CommandEnvelope(payload="")
        assert cmd.payload == ""

    def test_CommandEnvelope_InvalidType_ShouldRaise(self):
        with pytest.raises(ValidationError):
            CommandEnvelope(payload="test", type="invalid_type")
