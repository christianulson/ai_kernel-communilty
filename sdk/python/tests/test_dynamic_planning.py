from __future__ import annotations

import pytest

from krnlai.core.models.cognitive import CognitiveState
from krnlai.core.models.envelope import CommandEnvelope
from krnlai.core.steps.execution import ExecutionStep
from krnlai.core.steps.planning import DynamicPlanningStep


class TestDynamicPlanningStep:
    @pytest.mark.asyncio
    async def test_Execute_SimpleComplexity_ShouldReturnSingleStep(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.2, "thought_type": "unknown"})
        assert result["plan"] == ["execute"]
        assert result["total_steps"] == 1
        assert result["decomposition_strategy"] == "simple"

    @pytest.mark.asyncio
    async def test_Execute_ModerateComplexity_ShouldReturnThreeSteps(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5})
        assert result["plan"] == ["analyze", "execute", "verify"]
        assert result["total_steps"] == 3
        assert result["decomposition_strategy"] == "moderate"

    @pytest.mark.asyncio
    async def test_Execute_HighComplexity_ShouldReturnComplexDecomposition(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8})
        assert result["total_steps"] >= 4
        assert result["decomposition_strategy"] == "complex_decomposition"

    @pytest.mark.asyncio
    async def test_Execute_AnalyticalThoughtType_ShouldIncludeAnalyze(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "analytical"})
        assert "gather_data" in result["plan"]
        assert "analyze" in result["plan"]
        assert "conclude" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_CreativeThoughtType_ShouldIncludeDivergentExploration(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "creative"})
        assert "divergent_exploration" in result["plan"]
        assert "convergent_selection" in result["plan"]
        assert "refine" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_CriticalThoughtType_ShouldIncludeIdentifyAssumptions(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "critical"})
        assert "identify_assumptions" in result["plan"]
        assert "evaluate_evidence" in result["plan"]
        assert "formulate_conclusion" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_ProceduralThoughtType_ShouldIncludeLoadProcedure(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "procedural"})
        assert "load_procedure" in result["plan"]
        assert "execute_steps" in result["plan"]
        assert "verify_outcome" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_SocialThoughtType_ShouldIncludeConsiderPerspective(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "social"})
        assert "consider_perspective" in result["plan"]
        assert "formulate_response" in result["plan"]
        assert "empathy_check" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_HighFatigue_ShouldReducePlanSize(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "fatigue": 0.9})
        assert result["total_steps"] < 5
        assert "check_energy" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_HighNoveltyStarvation_ShouldAddExploreAlternatives(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.2, "novelty_starvation": 0.9})
        assert "explore_alternatives" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_DefaultThoughtType_ShouldReturnDefaultComplexDecomposition(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "unknown"})
        assert "research" in result["plan"]
        assert "decompose" in result["plan"]
        assert "integrate" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_HighCognitiveLoad_ShouldCheckWorkingMemory(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "cognitive_load": 0.8})
        assert "check_working_memory" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_MaxStepsLimit_ShouldNotExceedTen(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "thought_type": "creative"})
        assert result["total_steps"] <= 10

    @pytest.mark.asyncio
    async def test_Execute_FatigueAndNoveltyStarvation_ShouldBothApply(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        context = {"complexity": 0.6, "fatigue": 0.8, "novelty_starvation": 0.8}
        result = await planner.execute(cmd, state, context)
        assert "check_energy" in result["plan"]
        assert "explore_alternatives" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_HighRisk_ShouldAddValidateSafety(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "risk_score": 0.8})
        assert "validate_safety" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_HighRiskRequiresCaution_ShouldAddValidateSafety(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "metacognitive_observations": ["high_risk_requires_caution"]})
        assert "validate_safety" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_RequiresDecomposition_ShouldAddValidateSafety(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "requires_decomposition": True})
        assert "validate_safety" in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_LowRisk_ShouldNotAddValidateSafety(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "risk_score": 0.1})
        assert "validate_safety" not in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_EmptyPayload_ShouldStillPlan(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8})
        assert result["total_steps"] >= 1
        assert isinstance(result["plan"], list)

    @pytest.mark.asyncio
    async def test_Execute_ComplexityAtBoundary03_ShouldUseModerate(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.3, "thought_type": "unknown"})
        assert result["plan"] == ["analyze", "execute", "verify"]
        assert result["decomposition_strategy"] == "moderate"

    @pytest.mark.asyncio
    async def test_Execute_ComplexityAtBoundary07_ShouldUseComplex(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.7, "thought_type": "unknown"})
        assert result["decomposition_strategy"] == "complex_decomposition"
        assert result["total_steps"] >= 4

    @pytest.mark.asyncio
    async def test_Execute_CognitiveLoadAtBoundary05_ShouldNotCheckWorkingMemory(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "cognitive_load": 0.5})
        assert "check_working_memory" not in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_FatigueAtBoundary07_ShouldNotReduce(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "fatigue": 0.7})
        assert "check_energy" not in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_NoveltyStarvationAtBoundary07_ShouldNotAddExplore(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.8, "novelty_starvation": 0.7})
        assert "explore_alternatives" not in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_RiskScoreAtBoundary07_ShouldNotAddValidateSafety(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5, "risk_score": 0.7})
        assert "validate_safety" not in result["plan"]

    @pytest.mark.asyncio
    async def test_Execute_AllResultKeys_ShouldBePresent(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await planner.execute(cmd, state, {"complexity": 0.5})
        assert "plan" in result
        assert "current_step_index" in result
        assert "total_steps" in result
        assert "decomposition_strategy" in result


class TestExecutionStep:
    @pytest.mark.asyncio
    async def test_Execute_NoPlan_ShouldDefaultToExecute(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await executor.execute(cmd, state, {})
        assert "hello" in result["output"]
        assert result["execution_status"] == "success"

    @pytest.mark.asyncio
    async def test_Execute_WithPlan_ShouldExecuteSequentially(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["analyze", "execute", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert "Analyzed" in result["output"]
        assert "Executed" in result["output"]
        assert "Verified" in result["output"]
        assert result["steps_executed"] == ["analyze", "execute", "verify"]

    @pytest.mark.asyncio
    async def test_Execute_ComplexPlan_ShouldHandleAllStepTypes(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="data")
        state = CognitiveState()
        plan = ["gather_data", "analyze", "conclude", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert len(result["steps_executed"]) == 4
        assert "gather_data" in result["steps_executed"]

    @pytest.mark.asyncio
    async def test_Execute_WithCurrentIndex_ShouldSkipCompletedSteps(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["analyze", "execute", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan, "current_plan_step": 1})
        assert result["steps_executed"] == ["execute", "verify"]
        assert "Analyzed" not in result["output"]

    @pytest.mark.asyncio
    async def test_Execute_StepResults_ShouldContainStepNames(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["analyze", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert len(result["step_results"]) == 2
        assert result["step_results"][0]["step_name"] == "analyze"
        assert result["step_results"][0]["step_index"] == 0
        assert result["step_results"][1]["step_name"] == "verify"
        assert result["step_results"][1]["step_index"] == 1

    @pytest.mark.asyncio
    async def test_Execute_UnknownStep_ShouldUseGenericHandler(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["custom_step_xyz"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert "custom_step_xyz" in result["output"]
        assert result["execution_status"] == "unknown_step"

    @pytest.mark.asyncio
    async def test_Execute_MaxStepsLimit_ShouldRespectMax(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = [f"step_{i}" for i in range(20)]
        result = await executor.execute(cmd, state, {"plan": plan, "max_plan_steps": 5})
        assert len(result["steps_executed"]) == 5

    @pytest.mark.asyncio
    async def test_Execute_Timing_ShouldBeMeasured(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["analyze", "execute", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert isinstance(result["execution_time_ms"], float)
        assert result["execution_time_ms"] >= 0.0

    @pytest.mark.asyncio
    async def test_Execute_MixedKnownAndUnknown_ShouldExecuteAll(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        plan = ["analyze", "custom_unknown", "verify"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert result["steps_executed"] == ["analyze", "custom_unknown", "verify"]
        assert result["execution_status"] == "unknown_step"
        assert "Analyzed" in result["output"]
        assert "custom_unknown" in result["output"]

    @pytest.mark.asyncio
    async def test_Execute_EmptyPlan_ShouldDefaultToExecute(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()
        result = await executor.execute(cmd, state, {"plan": []})
        assert result["steps_executed"] == ["execute"]
        assert result["execution_status"] == "success"

    @pytest.mark.asyncio
    async def test_Execute_SingleStep_ShouldHandleCorrectly(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()
        result = await executor.execute(cmd, state, {"plan": ["validate_safety"]})
        assert result["steps_executed"] == ["validate_safety"]
        assert "Safety validated" in result["output"]

    @pytest.mark.asyncio
    async def test_Execute_AllKnownSteps_ShouldBeSuccess(self):
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="payload")
        state = CognitiveState()
        plan = ["analyze", "execute", "verify", "check_working_memory", "validate_safety",
                "gather_data", "conclude", "research", "decompose", "integrate"]
        result = await executor.execute(cmd, state, {"plan": plan})
        assert result["execution_status"] == "success"
        assert len(result["steps_executed"]) == 10


class TestIntegration:
    @pytest.mark.asyncio
    async def test_PlannerAndExecutor_SimplePlan_ShouldExecuteSuccessfully(self):
        planner = DynamicPlanningStep()
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="hello")
        state = CognitiveState()

        plan_result = await planner.execute(cmd, state, {"complexity": 0.2, "thought_type": "unknown"})
        exec_result = await executor.execute(cmd, state, {"plan": plan_result["plan"]})

        assert exec_result["steps_executed"] == ["execute"]
        assert "hello" in exec_result["output"]

    @pytest.mark.asyncio
    async def test_PlannerAndExecutor_ModeratePlan_ShouldExecuteAllSteps(self):
        planner = DynamicPlanningStep()
        executor = ExecutionStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()

        plan_result = await planner.execute(cmd, state, {"complexity": 0.5})
        exec_result = await executor.execute(cmd, state, {"plan": plan_result["plan"]})

        assert len(exec_result["steps_executed"]) == plan_result["total_steps"]

    @pytest.mark.asyncio
    async def test_PlannerAndExecutor_FatiguedPlan_ShouldBeShorter(self):
        planner = DynamicPlanningStep()
        cmd = CommandEnvelope(payload="test")
        state = CognitiveState()

        normal_result = await planner.execute(cmd, state, {"complexity": 0.8})
        tired_result = await planner.execute(cmd, state, {"complexity": 0.8, "fatigue": 0.9})

        assert tired_result["total_steps"] < normal_result["total_steps"]
        assert "check_energy" in tired_result["plan"]
