from __future__ import annotations

from krnlai.core.cognition.reasoning_quality import ReasoningQualityAssessor


class TestReasoningQualityAssessor:
    def test_Assess_EmptyText_ShouldHaveLowQuality(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("")
        assert result.quality >= 0.0
        assert "input_too_short_for_reasoning" in result.issues

    def test_Assess_ShortText_ShouldHaveIssues(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Hi")
        assert result.quality < 0.6
        assert "input_too_short_for_reasoning" in result.issues
        assert "missing_explicit_conclusion" in result.issues

    def test_Assess_RichReasoning_ShouldHaveHigherQuality(self):
        assessor = ReasoningQualityAssessor()
        text = ("Because the data shows a clear trend, therefore we can conclude "
                "that the hypothesis is correct. Furthermore, additional studies support this finding. "
                "However, more research is needed to confirm these results.")
        result = assessor.assess(text)
        assert result.quality >= 0.3
        assert "conclusion_provided" in result.strengths
        assert "evidence_support_present" in result.strengths

    def test_Assess_Coherence_Transitions_ShouldIncrease(self):
        assessor = ReasoningQualityAssessor()
        text_no_transitions = "First point. Second point. Third point."
        text_with_transitions = "First point. However, there is nuance. Therefore, we proceed."
        result_no = assessor.assess(text_no_transitions)
        result_with = assessor.assess(text_with_transitions)
        assert result_with.coherence >= result_no.coherence

    def test_Assess_Completeness_WithConclusionAndEvidence_ShouldBeHigher(self):
        assessor = ReasoningQualityAssessor()
        text = "Because of X, therefore Y is true."
        result = assessor.assess(text)
        assert result.completeness >= 0.5

    def test_Assess_Contradictions_ShouldDetectIssues(self):
        assessor = ReasoningQualityAssessor()
        text = "This is always true and never false."
        result = assessor.assess(text)
        assert "logical_contradiction_detected" in result.issues

    def test_Assess_Quality_Range_ShouldBeBetween0And1(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Test.")
        assert 0.0 <= result.quality <= 1.0

    def test_Assess_Coherence_Range_ShouldBeBetween0And1(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Test.")
        assert 0.0 <= result.coherence <= 1.0

    def test_Assess_Completeness_Range_ShouldBeBetween0And1(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Test.")
        assert 0.0 <= result.completeness <= 1.0
