from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List

from krnlai.core.cognition.bias_detector import BiasDetector, BiasFlag
from krnlai.core.cognition.cognitive_load import CognitiveLoadAssessor
from krnlai.core.cognition.reasoning_quality import ReasoningAssessment, ReasoningQualityAssessor
from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope


@dataclass
class CalibratedConfidence:
    confidence: float
    adjustment: float


class EnhancedMetacognitionStep:
    def __init__(
        self,
        bias_detector: BiasDetector | None = None,
        reasoning_quality: ReasoningQualityAssessor | None = None,
        cognitive_load: CognitiveLoadAssessor | None = None,
    ) -> None:
        self.bias_detector = bias_detector or BiasDetector()
        self.reasoning_quality = reasoning_quality or ReasoningQualityAssessor()
        self.cognitive_load = cognitive_load or CognitiveLoadAssessor()

    async def execute(self, cmd: CommandEnvelope, state: CognitiveState, context: Dict[str, Any]) -> Dict[str, Any]:
        reasoning = self.reasoning_quality.assess(cmd.payload, context)
        biases = self.bias_detector.detect(cmd.payload, context)
        calibrated = self._calibrate_confidence(
            initial_confidence=context.get("confidence", 0.5),
            reasoning_quality=reasoning.quality,
            bias_count=len(biases),
            emotional_state=state.emotional_state,
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
            "calibrated_confidence": calibrated.confidence,
            "calibration_adjustment": calibrated.adjustment,
            "cognitive_load": load.overall_load,
            "cognitive_load_intrinsic": load.intrinsic_load,
            "cognitive_load_extraneous": load.extraneous_load,
            "cognitive_load_germane": load.germane_load,
            "decision_type": decision_type,
            "requires_decomposition": load.overall_load > 0.7 or reasoning.quality < 0.3,
            "requires_intervention": len(biases) > 2 or reasoning.quality < 0.2,
        }

    @staticmethod
    def _calibrate_confidence(
        initial_confidence: float,
        reasoning_quality: float,
        bias_count: int,
        emotional_state: Dict[str, float] | None,
    ) -> CalibratedConfidence:
        adjustment = 0.0
        adjustment += (reasoning_quality - 0.5) * 0.3
        adjustment -= bias_count * 0.1
        if emotional_state:
            valence = emotional_state.get("valence", 0.0)
            arousal = emotional_state.get("arousal", 0.0)
            if abs(arousal) > 0.7:
                adjustment -= 0.15
            if valence > 0.8:
                adjustment -= 0.1
        adjustment = max(-0.5, min(0.5, adjustment))
        new_confidence = max(0.0, min(1.0, initial_confidence + adjustment))
        return CalibratedConfidence(confidence=round(new_confidence, 2), adjustment=round(adjustment, 2))

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
