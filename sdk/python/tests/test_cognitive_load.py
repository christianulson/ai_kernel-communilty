from __future__ import annotations

from krnlai.core.cognition.cognitive_load import CognitiveLoadAssessor


class TestCognitiveLoadAssessor:
    def test_Assess_SimpleInput_ShouldBeLow(self):
        assessor = CognitiveLoadAssessor()
        result = assessor.assess("Hello")
        assert result.overall_load < 0.5
        assert result.intrinsic_load < 0.5

    def test_Assess_ComplexInput_ShouldBeHigher(self):
        assessor = CognitiveLoadAssessor()
        simple = assessor.assess("Hello")
        complex_text = ("Analyze the algorithm architecture and mathematical foundations "
                        "of the distributed computing system. Because of these factors, "
                        "we can evaluate the overall performance. Furthermore, the implications "
                        "for system design are significant. However, there are trade-offs to consider.")
        complex = assessor.assess(complex_text)
        assert complex.overall_load >= simple.overall_load

    def test_Assess_Extraneous_Redundant_ShouldIncrease(self):
        assessor = CognitiveLoadAssessor()
        clean = assessor.assess("Hello world")
        redundant = assessor.assess("This is vague and unclear. The confusing part is the redundant section.")
        assert redundant.extraneous_load >= clean.extraneous_load

    def test_Assess_Germane_Novelty_ShouldIncrease(self):
        assessor = CognitiveLoadAssessor()
        low = assessor.assess("test", homeostasis_state={"novelty": 0.0})
        high = assessor.assess("test", homeostasis_state={"novelty": 0.8})
        assert high.germane_load >= low.germane_load

    def test_Assess_Germane_ContextRichness_ShouldIncrease(self):
        assessor = CognitiveLoadAssessor()
        low = assessor.assess("test", context={"a": 1})
        high = assessor.assess("test", context={"a": 1, "b": 2, "c": 3, "d": 4, "e": 5, "f": 6})
        assert high.germane_load >= low.germane_load

    def test_Assess_AllComponents_Range(self):
        assessor = CognitiveLoadAssessor()
        result = assessor.assess("Analyze the algorithm complexity")
        assert 0.0 <= result.overall_load <= 1.0
        assert 0.0 <= result.intrinsic_load <= 1.0
        assert 0.0 <= result.extraneous_load <= 1.0
        assert 0.0 <= result.germane_load <= 1.0

    def test_Assess_DomainKeywords_ShouldIncrease(self):
        assessor = CognitiveLoadAssessor()
        general = assessor.assess("Hello world")
        domain = assessor.assess("The code algorithm uses advanced mathematics")
        assert domain.intrinsic_load >= general.intrinsic_load

    def test_Assess_Fatigue_ShouldIncreaseOverall(self):
        assessor = CognitiveLoadAssessor()
        no_fatigue = assessor.assess("test text here")
        fatigued = assessor.assess("test text here", homeostasis_state={"fatigue": 0.8})
        assert fatigued.overall_load >= no_fatigue.overall_load
