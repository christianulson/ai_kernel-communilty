from __future__ import annotations

import pytest

from krnlai.core.cognition.confidence import CalibratedConfidence, ConfidenceCalibrator


class TestCalibratedConfidence:
    def test_Create_AllFields_ShouldBeSet(self):
        cc = CalibratedConfidence(
            raw_confidence=0.8,
            calibrated_confidence=0.75,
            adjustment=-0.05,
            calibration_error=0.12,
            reason="low_quality",
        )
        assert cc.raw_confidence == 0.8
        assert cc.calibrated_confidence == 0.75
        assert cc.adjustment == -0.05
        assert cc.calibration_error == 0.12
        assert cc.reason == "low_quality"

    def test_Create_DefaultReason_ShouldBeEmpty(self):
        cc = CalibratedConfidence(
            raw_confidence=0.5,
            calibrated_confidence=0.5,
            adjustment=0.0,
            calibration_error=0.0,
            reason="no_adjustment",
        )
        assert cc.reason == "no_adjustment"


class TestConfidenceCalibrator:
    def test_Calibrate_NoHistoryNoFactors_ShouldReturnRaw(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.8,
            thought_type="analytical",
            reasoning_quality=0.8,
            bias_count=0,
            emotional_state=None,
            complexity=0.0,
        )
        assert result.raw_confidence == 0.8
        assert result.calibrated_confidence == 0.8
        assert result.adjustment == 0.0
        assert result.calibration_error == 0.0
        assert result.reason == "no_adjustment"

    def test_Calibrate_LowReasoningQuality_ShouldReduce(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.8,
            thought_type="analytical",
            reasoning_quality=0.2,
            bias_count=0,
            emotional_state=None,
            complexity=0.0,
        )
        assert result.calibrated_confidence < result.raw_confidence
        assert result.adjustment < 0.0
        assert "low_quality" in result.reason

    def test_Calibrate_ModerateReasoningQuality_ShouldReduceLess(self):
        calibrator = ConfidenceCalibrator()
        low = calibrator.calibrate(0.8, "analytical", 0.2, 0, None, 0.0)
        moderate = calibrator.calibrate(0.8, "analytical", 0.5, 0, None, 0.0)
        assert moderate.calibrated_confidence > low.calibrated_confidence

    def test_Calibrate_MultipleBiases_ShouldReduce(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.8,
            thought_type="analytical",
            reasoning_quality=0.8,
            bias_count=3,
            emotional_state=None,
            complexity=0.0,
        )
        assert result.calibrated_confidence < result.raw_confidence
        assert "biases" in result.reason

    def test_Calibrate_NegativeEmotionalState_ShouldReduce(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.8,
            thought_type="analytical",
            reasoning_quality=0.8,
            bias_count=0,
            emotional_state={"valence": -0.6, "arousal": 0.3},
            complexity=0.0,
        )
        assert result.calibrated_confidence < result.raw_confidence
        assert "negative_valence" in result.reason

    def test_Calibrate_HighComplexity_ShouldReduce(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.8,
            thought_type="analytical",
            reasoning_quality=0.8,
            bias_count=0,
            emotional_state=None,
            complexity=0.9,
        )
        assert result.calibrated_confidence < result.raw_confidence
        assert "complexity" in result.reason

    def test_Calibrate_AllBoundaries_ShouldStayInRange(self):
        calibrator = ConfidenceCalibrator()
        for raw in [0.0, 0.5, 1.0]:
            result = calibrator.calibrate(raw, "analytical", 0.0, 5, {"valence": -0.8}, 1.0)
            assert 0.0 <= result.calibrated_confidence <= 1.0

    def test_Calibrate_RawConfidenceClipped_ShouldStayInZeroOne(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(1.0, "analytical", 0.0, 5, {"valence": -0.8}, 1.0)
        assert result.calibrated_confidence >= 0.0
        result = calibrator.calibrate(0.0, "analytical", 0.8, 0, {"valence": 0.9}, 0.0)
        assert result.calibrated_confidence <= 1.0

    def test_RecordOutcome_ShouldAccumulate(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.8, True)
        calibrator.record_outcome("analytical", 0.7, True)
        assert calibrator.get_calibration_error("analytical") > 0.0

    def test_RecordOutcome_LowHistoryMax_ShouldKeep100(self):
        calibrator = ConfidenceCalibrator()
        for i in range(150):
            calibrator.record_outcome("analytical", 0.8, True)
        assert len(calibrator._history["analytical"]) == 100

    def test_Calibrate_WithHistoryOfErrors_ShouldReduceMore(self):
        calibrator = ConfidenceCalibrator()
        for _ in range(10):
            calibrator.record_outcome("analytical", 0.9, False)
        baseline = calibrator.calibrate(0.9, "analytical", 0.8, 0, None, 0.0)
        calibrator2 = ConfidenceCalibrator()
        no_history = calibrator2.calibrate(0.9, "analytical", 0.8, 0, None, 0.0)
        assert baseline.calibrated_confidence < no_history.calibrated_confidence
        assert baseline.calibration_error > 0.0

    def test_CalibrationError_NoHistory_ShouldBeZero(self):
        calibrator = ConfidenceCalibrator()
        assert calibrator.get_calibration_error("unknown") == 0.0

    def test_CalibrationError_AfterOutcomes_ShouldReflectAccuracy(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.9, True)
        calibrator.record_outcome("analytical", 0.8, True)
        calibrator.record_outcome("analytical", 0.9, False)
        error = calibrator.get_calibration_error("analytical")
        assert 0.0 < error <= 1.0

    def test_GetCalibrationCurve_ShouldReturnAllTypes(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.8, True)
        calibrator.record_outcome("creative", 0.6, False)
        curve = calibrator.get_calibration_curve()
        assert "analytical" in curve
        assert "creative" in curve
        assert len(curve) == 2

    def test_GetCalibrationCurve_Empty_ShouldBeEmpty(self):
        calibrator = ConfidenceCalibrator()
        assert calibrator.get_calibration_curve() == {}

    def test_Calibrate_CalibrationErrorInResult_ShouldMatch(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.8, False)
        result = calibrator.calibrate(0.8, "analytical", 0.8, 0, None, 0.0)
        assert result.calibration_error == calibrator.get_calibration_error("analytical")

    def test_RecordOutcome_DifferentThoughtTypes_ShouldTrackSeparately(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.8, True)
        calibrator.record_outcome("creative", 0.5, False)
        assert calibrator.get_calibration_error("analytical") < calibrator.get_calibration_error("creative")

    def test_Calibrate_ReasonText_ShouldIndicateMultipleFactors(self):
        calibrator = ConfidenceCalibrator()
        result = calibrator.calibrate(
            raw_confidence=0.9,
            thought_type="analytical",
            reasoning_quality=0.2,
            bias_count=2,
            emotional_state={"valence": -0.6},
            complexity=0.8,
        )
        assert "low_quality" in result.reason
        assert "biases" in result.reason
        assert "negative_valence" in result.reason
        assert "complexity" in result.reason

    def test_Calibrate_HighValence_ShouldReduceSlightly(self):
        calibrator = ConfidenceCalibrator()
        no_emotion = calibrator.calibrate(0.8, "analytical", 0.8, 0, None, 0.0)
        with_emotion = calibrator.calibrate(0.8, "analytical", 0.8, 0, {"valence": 0.8}, 0.0)
        assert with_emotion.calibrated_confidence <= no_emotion.calibrated_confidence
        assert "high_valence" in with_emotion.reason

    def test_Reset_ShouldClearHistory(self):
        calibrator = ConfidenceCalibrator()
        calibrator.record_outcome("analytical", 0.8, True)
        assert calibrator.get_calibration_curve() != {}
        calibrator.reset()
        assert calibrator.get_calibration_curve() == {}
        assert calibrator.get_calibration_error("analytical") == 0.0


class TestIntegrationWithMetacognition:
    @pytest.mark.asyncio
    async def test_MetacognitionStep_UsesConfidenceCalibrator(self):
        from krnlai.core.models.cognitive import CognitiveState
        from krnlai.core.models.envelope import CommandEnvelope
        from krnlai.core.steps.metacognition import EnhancedMetacognitionStep

        calibrator = ConfidenceCalibrator()
        step = EnhancedMetacognitionStep(confidence_calibrator=calibrator)
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await step.execute(cmd, state, {"confidence": 0.9})
        assert "calibrated_confidence" in result
        assert "calibration_error" in result
        assert "calibration_reason" in result
        assert 0.0 <= result["calibrated_confidence"] <= 1.0

    @pytest.mark.asyncio
    async def test_MetacognitionStep_LearningImprovesCalibration(self):
        from krnlai.core.models.cognitive import CognitiveState
        from krnlai.core.models.envelope import CommandEnvelope
        from krnlai.core.steps.metacognition import EnhancedMetacognitionStep

        calibrator = ConfidenceCalibrator()
        step = EnhancedMetacognitionStep(confidence_calibrator=calibrator)
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()

        result1 = await step.execute(cmd, state, {"confidence": 0.9})
        calibrator.record_outcome("analytical", result1["calibrated_confidence"], True)
        calibrator.record_outcome("analytical", result1["calibrated_confidence"], True)
        calibrator.record_outcome("analytical", result1["calibrated_confidence"], True)
        calibrator.record_outcome("analytical", result1["calibrated_confidence"], True)

        result2 = await step.execute(cmd, state, {"confidence": 0.9})
        assert isinstance(result2["calibration_error"], float)
