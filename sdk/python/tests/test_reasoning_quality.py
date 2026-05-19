from __future__ import annotations

from krnlai.core.cognition.reasoning_quality import ReasoningIssue, ReasoningQualityAssessor


class TestReasoningIssue:
    def test_Enum_AllMembers_ShouldBe10(self):
        assert len(ReasoningIssue) == 10


class TestReasoningQualityAssessor:
    def test_Assess_EmptyText_ShouldHaveLowQuality(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("", "")
        assert result.quality >= 0.0
        assert "input_too_short_for_reasoning" in result.issues

    def test_Assess_ShortText_ShouldHaveIssues(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Hi")
        assert result.quality < 0.6
        assert "input_too_short_for_reasoning" in result.issues

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
        text_no = "First point. Second point. Third point."
        text_with = "First point. However, there is nuance. Therefore, we proceed."
        result_no = assessor.assess(text_no)
        result_with = assessor.assess(text_with)
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

    def test_Assess_Soundness_Range_ShouldBeBetween0And1(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("Test.")
        assert 0.0 <= result.soundness <= 1.0

    def test_Assess_WithOutputText_ShouldIncludeInAnalysis(self):
        assessor = ReasoningQualityAssessor()
        result = assessor.assess("short", "because of evidence therefore conclusion")
        assert "evidence_support_present" in result.strengths

    def test_DetectFallacies_CircularReasoning_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "This is true because it is true by definition."
        result = assessor.assess(text)
        assert ReasoningIssue.CIRCULAR_REASONING.value in result.issues

    def test_DetectFallacies_HastyGeneralization_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "Everyone knows that this is always the case."
        result = assessor.assess(text)
        assert ReasoningIssue.HASTY_GENERALIZATION.value in result.issues

    def test_DetectFallacies_FalseCause_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "The accident was caused by the rain, because of the weather."
        result = assessor.assess(text)
        assert ReasoningIssue.FALSE_CAUSE.value in result.issues

    def test_DetectFallacies_AppealToAuthority_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "According to experts, this is true."
        result = assessor.assess(text)
        assert ReasoningIssue.APPEAL_TO_AUTHORITY.value in result.issues

    def test_DetectFallacies_StrawMan_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "So you think that your argument is valid."
        result = assessor.assess(text)
        assert ReasoningIssue.STRAW_MAN.value in result.issues

    def test_DetectFallacies_FalseDilemma_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "There is no other option, either or."
        result = assessor.assess(text)
        assert ReasoningIssue.FALSE_DILEMMA.value in result.issues

    def test_DetectFallacies_SlipperySlope_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "First then this happens, then eventually that follows."
        result = assessor.assess(text)
        assert ReasoningIssue.SLIPPERY_SLOPE.value in result.issues

    def test_DetectFallacies_BeggingQuestion_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "Obviously this is clearly the correct answer."
        result = assessor.assess(text)
        assert ReasoningIssue.BEGGING_QUESTION.value in result.issues

    def test_DetectFallacies_CherryPicking_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "These selectively chosen examples show only some cases."
        result = assessor.assess(text)
        assert ReasoningIssue.CHERRY_PICKING.value in result.issues

    def test_DetectFallacies_FalseAnalogy_ShouldDetect(self):
        assessor = ReasoningQualityAssessor()
        text = "This situation is like when something similar happened."
        result = assessor.assess(text)
        assert ReasoningIssue.FALSE_ANALOGY.value in result.issues

    def test_Assess_ShouldReturnAssumptions(self):
        assessor = ReasoningQualityAssessor()
        text = "Assume that this is true, given that we have evidence."
        result = assessor.assess(text)
        assert len(result.assumptions) > 0
        assert any("assume" in a for a in result.assumptions)

    def test_Assess_ShouldReturnMissingContext(self):
        assessor = ReasoningQualityAssessor()
        text = "We need more data to analyze this."
        result = assessor.assess(text)
        assert "missing_context:data" in result.missing_context

    def test_Assess_WithContext_ShouldNotFlagAsMissing(self):
        assessor = ReasoningQualityAssessor()
        text = "We need more data to analyze this."
        result = assessor.assess(text, context={"data": "some values"})
        assert "missing_context:data" not in result.missing_context

    def test_Assess_Soundness_StrongIndicators_ShouldBeHigher(self):
        assessor = ReasoningQualityAssessor()
        weak = assessor.assess("Maybe it could be something.")
        strong = assessor.assess("Therefore, because of the evidence, the conclusion follows.")
        assert strong.soundness >= weak.soundness

    def test_Assess_MultipleFallacies_ShouldDetectAll(self):
        assessor = ReasoningQualityAssessor()
        text = "Everyone knows this is true. Obviously, experts say so."
        result = assessor.assess(text)
        assert ReasoningIssue.HASTY_GENERALIZATION.value in result.issues
        assert ReasoningIssue.APPEAL_TO_AUTHORITY.value in result.issues
        assert ReasoningIssue.BEGGING_QUESTION.value in result.issues
