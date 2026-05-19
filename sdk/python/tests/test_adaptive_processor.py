from __future__ import annotations

from krnlai.core.cognition.adaptive import AdaptiveProcessor
from krnlai.core.cycle import CycleConfig


class TestProcessingMode:
    def test_Create_DefaultConfig_ShouldHaveNORMAL(self):
        proc = AdaptiveProcessor()
        mode = proc.current_mode
        assert mode.mode == "NORMAL"
        assert mode.max_iterations == 10
        assert mode.safety_level == "strict"


class TestAdaptiveProcessor:
    def test_DetermineMode_NormalState_ShouldReturnNORMAL(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.6,
            bias_count=1,
            calibration_error=0.1,
            cognitive_load=0.4,
            fatigue=0.3,
        )
        assert mode.mode == "NORMAL"

    def test_DetermineMode_HighFatigueLowQuality_ShouldReturnRECOVERY(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.2,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.5,
            fatigue=0.9,
        )
        assert mode.mode == "RECOVERY"
        assert mode.max_iterations == 1
        assert mode.safety_level == "strict"

    def test_DetermineMode_FatigueAtBoundary08HighQuality_ShouldNotRecover(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.8,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.3,
            fatigue=0.8,
        )
        assert mode.mode != "RECOVERY"

    def test_DetermineMode_LowQuality_ShouldReturnCONSERVATIVE(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.3,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.3,
            fatigue=0.2,
        )
        assert mode.mode == "CONSERVATIVE"
        assert mode.max_iterations == 3
        assert mode.novelty_seeking == 0.1

    def test_DetermineMode_QualityAtBoundary04_ShouldNotBeConservative(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.4,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.3,
            fatigue=0.2,
        )
        assert mode.mode != "CONSERVATIVE"

    def test_DetermineMode_ManyBiases_ShouldReturnCONSERVATIVE(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.6,
            bias_count=4,
            calibration_error=0.0,
            cognitive_load=0.3,
            fatigue=0.2,
        )
        assert mode.mode == "CONSERVATIVE"

    def test_DetermineMode_HighQualityLowLoad_ShouldReturnEXPLORATORY(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.9,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.2,
            fatigue=0.1,
        )
        assert mode.mode == "EXPLORATORY"
        assert mode.max_iterations == 15
        assert mode.safety_level == "relaxed"
        assert mode.novelty_seeking == 0.8

    def test_DetermineMode_HighQualityButHighLoad_ShouldNotExplore(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.9,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.5,
            fatigue=0.1,
        )
        assert mode.mode != "EXPLORATORY"

    def test_DetermineMode_QualityAtBoundary08_ShouldNotExplore(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(
            reasoning_quality=0.8,
            bias_count=0,
            calibration_error=0.0,
            cognitive_load=0.9,
            fatigue=0.1,
        )
        assert mode.mode != "EXPLORATORY"

    def test_RecordAssessment_ShouldAccumulate(self):
        proc = AdaptiveProcessor()
        proc.record_assessment(0, 0.8, 0.7, 0.9, 0.8, [])
        proc.record_assessment(1, 0.6, 0.5, 0.7, 0.6, ["circular"])
        assert len(proc.history) == 2

    def test_GetAverageQuality_ShouldComputeCorrectly(self):
        proc = AdaptiveProcessor()
        proc.record_assessment(0, 0.8, 0.7, 0.9, 0.8, [])
        proc.record_assessment(1, 0.6, 0.5, 0.7, 0.6, [])
        assert proc.get_average_quality() == 0.7

    def test_GetAverageQuality_Empty_ShouldBeZero(self):
        proc = AdaptiveProcessor()
        assert proc.get_average_quality() == 0.0

    def test_GetHistory_ShouldRespectLimit(self):
        proc = AdaptiveProcessor()
        for i in range(20):
            proc.record_assessment(i, 0.5, 0.5, 0.5, 0.5, [])
        assert len(proc.get_history(5)) == 5
        assert len(proc.get_history(100)) == 20

    def test_ApplyModeToConfig_ShouldUpdateConfig(self):
        config = CycleConfig(max_iterations=10, safety_level="strict")
        proc = AdaptiveProcessor(config)
        proc.determine_mode(0.2, 0, 0.0, 0.5, 0.9)
        proc.apply_mode_to_config(config)
        assert config.max_iterations == 1
        assert config.safety_level == "strict"

    def test_DetermineMode_Exploratory_ShouldSetRelaxedSafety(self):
        config = CycleConfig(max_iterations=10, safety_level="strict")
        proc = AdaptiveProcessor(config)
        proc.determine_mode(0.9, 0, 0.0, 0.2, 0.1)
        proc.apply_mode_to_config(config)
        assert config.safety_level == "relaxed"

    def test_CurrentMode_Property_ShouldReflectLastDetermination(self):
        proc = AdaptiveProcessor()
        proc.determine_mode(0.2, 0, 0.0, 0.5, 0.9)
        assert proc.current_mode.mode == "RECOVERY"
        proc.determine_mode(0.6, 1, 0.0, 0.4, 0.3)
        assert proc.current_mode.mode == "NORMAL"

    def test_Reset_ShouldClearHistoryAndMode(self):
        proc = AdaptiveProcessor()
        proc.determine_mode(0.2, 0, 0.0, 0.5, 0.9)
        proc.record_assessment(0, 0.2, 0.3, 0.4, 0.5, [])
        proc.reset()
        assert len(proc.history) == 0
        assert proc.current_mode.mode == "NORMAL"

    def test_RecordAssessment_ShouldStoreMode(self):
        proc = AdaptiveProcessor()
        proc.determine_mode(0.2, 0, 0.0, 0.5, 0.9)
        proc.record_assessment(0, 0.2, 0.3, 0.4, 0.5, ["circular"])
        assert proc.history[0].mode == "RECOVERY"
        assert proc.history[0].issues == ["circular"]

    def test_DetermineMode_EdgeCaseQualityZero_ShouldBeConservative(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(0.0, 0, 0.0, 0.3, 0.2)
        assert mode.mode == "CONSERVATIVE"

    def test_DetermineMode_EdgeCaseFatigueOne_ShouldBeRecovery(self):
        proc = AdaptiveProcessor()
        mode = proc.determine_mode(0.2, 0, 0.0, 0.3, 1.0)
        assert mode.mode == "RECOVERY"
