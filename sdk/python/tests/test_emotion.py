from __future__ import annotations

from aikernel.core.emotion.pain_reward import PainRewardModel, PainRewardSignal
from aikernel.core.emotion.vad import VADModel, VADState


class TestVADState:
    def test_Default_ShouldBeZero(self):
        v = VADState()
        assert v.valence == 0.0
        assert v.arousal == 0.0
        assert v.dominance == 0.0

    def test_IsPositive_WithPositiveValence_ShouldBeTrue(self):
        v = VADState(valence=0.5)
        assert v.is_positive is True
        assert v.is_negative is False

    def test_IsNegative_WithNegativeValence_ShouldBeTrue(self):
        v = VADState(valence=-0.5)
        assert v.is_negative is True
        assert v.is_positive is False

    def test_IsCalm_LowArousal_ShouldBeTrue(self):
        v = VADState(arousal=0.1)
        assert v.is_calm is True
        assert v.is_intense is False

    def test_DistanceTo_Identical_ShouldBeZero(self):
        v1 = VADState(0.5, 0.3, 0.1)
        v2 = VADState(0.5, 0.3, 0.1)
        assert v1.distance_to(v2) == 0.0

    def test_DistanceTo_Different_ShouldBePositive(self):
        v1 = VADState(0.0, 0.0, 0.0)
        v2 = VADState(1.0, 1.0, 1.0)
        assert v1.distance_to(v2) > 0.0

    def test_ToDict_ShouldMatch(self):
        v = VADState(valence=0.5, arousal=-0.3, dominance=0.1)
        d = v.to_dict()
        assert d["valence"] == 0.5
        assert d["arousal"] == -0.3
        assert d["dominance"] == 0.1


class TestVADModel:
    def test_Initial_Default_ShouldBeZero(self):
        model = VADModel()
        assert model.current.valence == 0.0

    def test_Update_PositiveDelta_ShouldIncrease(self):
        model = VADModel()
        model.update(delta_valence=0.5, trigger="happy")
        assert model.current.valence == 0.5

    def test_Update_ReturnsTransition_ShouldHaveDelta(self):
        model = VADModel()
        transition = model.update(delta_valence=0.3, delta_arousal=0.2, trigger="test")
        assert transition.delta["valence"] == 0.3
        assert transition.delta["arousal"] == 0.2

    def test_Decay_MultipleSteps_ShouldReduce(self):
        model = VADModel(initial_state=VADState(valence=1.0, arousal=1.0, dominance=1.0))
        model.decay(steps=5)
        assert model.current.valence < 1.0
        assert model.current.arousal < 1.0

    def test_ModulateRisk_NegativeValence_ShouldIncrease(self):
        model = VADModel(initial_state=VADState(valence=-0.8, arousal=0.5))
        modulated = model.modulate_risk(0.5)
        assert modulated > 0.5

    def test_History_AfterUpdates_ShouldTrack(self):
        model = VADModel()
        model.update(delta_valence=0.3, trigger="a")
        model.update(delta_valence=-0.1, trigger="b")
        assert len(model.history) == 2
        assert model.history[0].trigger == "a"
        assert model.history[1].trigger == "b"

    def test_Reset_ShouldClear(self):
        model = VADModel(initial_state=VADState(valence=0.8))
        model.update(delta_valence=0.2, trigger="test")
        model.reset()
        assert model.current.valence == 0.0
        assert len(model.history) == 0


class TestPainRewardModel:
    def test_Initial_ShouldBeZero(self):
        prm = PainRewardModel()
        assert prm.net_hedonic_value == 0.0
        assert prm.pain_level == 0.0
        assert prm.reward_level == 0.0

    def test_ApplyReward_ShouldIncrease(self):
        prm = PainRewardModel(learning_rate=1.0)
        signal = PainRewardSignal.reward(1.0, "good")
        prm.apply(signal)
        assert prm.reward_level == 1.0
        assert prm.net_hedonic_value == 1.0

    def test_ApplyPain_ShouldDecreaseNet(self):
        prm = PainRewardModel(learning_rate=1.0)
        prm.apply(PainRewardSignal.reward(2.0, "good"))
        prm.apply(PainRewardSignal.pain(1.0, "bad"))
        assert prm.net_hedonic_value == 1.0
        assert prm.pain_level == 1.0

    def test_Reset_ShouldClear(self):
        prm = PainRewardModel(learning_rate=1.0)
        prm.apply(PainRewardSignal.reward(1.0))
        prm.reset()
        assert prm.net_hedonic_value == 0.0
        assert len(prm.history) == 0

    def test_History_ShouldTrackAll(self):
        prm = PainRewardModel()
        signals = [
            PainRewardSignal.reward(0.5, "a"),
            PainRewardSignal.pain(0.3, "b"),
            PainRewardSignal.reward(0.2, "c"),
        ]
        prm.apply_many(signals)
        assert len(prm.history) == 3
