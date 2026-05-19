from __future__ import annotations

from typing import Any, Dict, List, Optional

from krnlai.core.cognition.bias_detector import BiasDetector, BiasFlag
from krnlai.core.cognition.cognitive_load import CognitiveLoadAssessor
from krnlai.core.cognition.confidence import ConfidenceCalibrator
from krnlai.core.cognition.reasoning_quality import ReasoningAssessment, ReasoningQualityAssessor
from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


class EnhancedMetacognitionStep:
    def __init__(
        self,
        bias_detector: Optional[BiasDetector] = None,
        reasoning_quality: Optional[ReasoningQualityAssessor] = None,
        cognitive_load: Optional[CognitiveLoadAssessor] = None,
        confidence_calibrator: Optional[ConfidenceCalibrator] = None,
    ) -> None:
        self.bias_detector = bias_detector or BiasDetector()
        self.reasoning_quality = reasoning_quality or ReasoningQualityAssessor()
        self.cognitive_load = cognitive_load or CognitiveLoadAssessor()
        self.confidence_calibrator = confidence_calibrator or ConfidenceCalibrator()

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        reasoning = self.reasoning_quality.assess(cmd.payload, "", context)
        biases = self.bias_detector.detect(cmd.payload, context)
        thought_type = context.get("thought_type", "analytical")

        calibrated = self.confidence_calibrator.calibrate(
            raw_confidence=context.get("confidence", 0.5),
            thought_type=thought_type,
            reasoning_quality=reasoning.quality,
            bias_count=len(biases),
            emotional_state=state.emotional_state,
            complexity=context.get("complexity", 0.0),
        )

        load = self.cognitive_load.assess(
            payload=cmd.payload,
            context=context,
            homeostasis_state={"novelty": context.get("novelty", 0.0), "fatigue": context.get("fatigue", 0.0)},
        )
        decision_type = self._classify_decision_type(context)

        return {
            "observations": self._build_observations(reasoning, biases, load),
            "reasoning_quality": reasoning.quality,
            "reasoning_issues": reasoning.issues,
            "reasoning_strengths": reasoning.strengths,
            "reasoning_coherence": reasoning.coherence,
            "reasoning_completeness": reasoning.completeness,
            "biases_detected": [
                {"type": b.bias_type.value, "severity": b.severity, "evidence": b.evidence}
                for b in biases
            ],
            "calibrated_confidence": calibrated.calibrated_confidence,
            "calibration_adjustment": calibrated.adjustment,
            "calibration_error": calibrated.calibration_error,
            "calibration_reason": calibrated.reason,
            "cognitive_load": load.overall_load,
            "cognitive_load_intrinsic": load.intrinsic_load,
            "cognitive_load_extraneous": load.extraneous_load,
            "cognitive_load_germane": load.germane_load,
            "decision_type": decision_type,
            "requires_decomposition": load.overall_load > 0.7 or reasoning.quality < 0.3,
            "requires_intervention": len(biases) > 2 or reasoning.quality < 0.2,
        }

    @staticmethod
    def _classify_decision_type(context: Dict[str, Any]) -> str:
        risk = context.get("risk_score", 0.0)
        urgency = context.get("urgency", 0.0)
        complexity = context.get("complexity", 0.0)

        if risk > 0.7:
            return "high_risk"
        if urgency > 0.7:
            return "time_sensitive"
        if complexity > 0.7:
            return "complex"
        if risk > 0.3:
            return "moderate_risk"
        return "routine"

    @staticmethod
    def _build_observations(
        reasoning: ReasoningAssessment,
        biases: List[BiasFlag],
        load: Any,
    ) -> List[str]:
        observations: list[str] = []
        if reasoning.quality < 0.3:
            observations.append("poor_reasoning_quality")
        elif reasoning.quality < 0.6:
            observations.append("moderate_reasoning_quality")
        else:
            observations.append("good_reasoning_quality")
        if reasoning.issues:
            observations.append(f"reasoning_issues:{len(reasoning.issues)}")
        if biases:
            observations.append(f"biases_detected:{len(biases)}")
        if load.overall_load > 0.7:
            observations.append("high_cognitive_load")
        elif load.overall_load > 0.4:
            observations.append("moderate_cognitive_load")
        return observations
