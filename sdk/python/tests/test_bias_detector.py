from __future__ import annotations

from krnlai.core.cognition.bias_detector import BiasDetector, BiasType


class TestBiasDetector:
    def test_Detect_CleanText_ShouldReturnEmpty(self):
        detector = BiasDetector()
        flags = detector.detect("What is the capital of France?")
        assert len(flags) == 0

    def test_Detect_ConfirmationBias_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("As I thought, this clearly shows I was right all along.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.CONFIRMATION in bias_types

    def test_Detect_Overconfidence_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("This is definitely the correct answer without a doubt.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.OVERCONFIDENCE in bias_types

    def test_Detect_Anchoring_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("Based on the initial assessment, we should proceed.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.ANCHORING in bias_types

    def test_Detect_Hindsight_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("I knew it all along, it was obvious in hindsight.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.HINDSIGHT in bias_types

    def test_Detect_Negativity_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("This is a dangerous problem and a critical error.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.NEGATIVITY in bias_types

    def test_Detect_GroupThink_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("The consensus is that everyone agrees with the team decision.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.GROUP_THINK in bias_types

    def test_Detect_Availability_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("I recall a recent example that is a typical case of this.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.AVAILABILITY in bias_types

    def test_Detect_Framing_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("The gain from this opportunity outweighs the risk of loss.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.FRAMING in bias_types

    def test_Detect_Recency_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("The latest and most recent data shows a new trend.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.RECENCY in bias_types

    def test_Detect_AffectHeuristic_ShouldDetect(self):
        detector = BiasDetector()
        flags = detector.detect("My gut instinct tells me this feels right.")
        bias_types = [f.bias_type for f in flags]
        assert BiasType.AFFECT_HEURISTIC in bias_types

    def test_Detect_MultipleBiases_ShouldReturnAll(self):
        detector = BiasDetector()
        flags = detector.detect("As I thought, this definitely proves my theory. Everyone agrees with me.")
        assert len(flags) >= 2

    def test_Detect_Severity_ShouldScaleWithMatches(self):
        detector = BiasDetector()
        flags_single = detector.detect("I knew this would happen.")
        flags_multi = detector.detect("I knew this would happen. As expected, clearly shows I was right.")
        severity_single = next((f.severity for f in flags_single if f.bias_type == BiasType.CONFIRMATION), 0.0)
        severity_multi = next((f.severity for f in flags_multi if f.bias_type == BiasType.CONFIRMATION), 0.0)
        assert severity_multi >= severity_single

    def test_Detect_BiasFlag_HasEvidence(self):
        detector = BiasDetector()
        flags = detector.detect("I knew this would happen.")
        if flags:
            assert len(flags[0].evidence) > 0

    def test_BiasType_Enum_AllValues_ShouldBeAccessible(self):
        assert BiasType.CONFIRMATION.value == "confirmation_bias"
        assert BiasType.ANCHORING.value == "anchoring"
        assert BiasType.AVAILABILITY.value == "availability"
        assert BiasType.OVERCONFIDENCE.value == "overconfidence"
        assert BiasType.FRAMING.value == "framing"
        assert BiasType.HINDSIGHT.value == "hindsight"
        assert BiasType.RECENCY.value == "recency"
        assert BiasType.NEGATIVITY.value == "negativity"
        assert BiasType.AFFECT_HEURISTIC.value == "affect_heuristic"
        assert BiasType.GROUP_THINK.value == "group_think"

    def test_BiasType_Count_ShouldHave10Values(self):
        assert len(BiasType) == 10
