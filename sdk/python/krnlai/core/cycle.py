from __future__ import annotations

import asyncio
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any, AsyncGenerator, Callable, Dict, List, Optional

from krnlai.core.cognition.adaptive import AdaptiveProcessor
from krnlai.core.cognition.confidence import ConfidenceCalibrator
from krnlai.core.cognition.reasoning_quality import ReasoningQualityAssessor
from krnlai.core.cognition.thought_graph import ThoughtGraph, ThoughtNode, ThoughtRelation
from krnlai.core.emotion.pain_reward import PainRewardModel
from krnlai.core.emotion.vad import VADModel
from krnlai.core.memory.episodic_memory import EpisodicMemory
from krnlai.core.memory.semantic_memory import SemanticMemory
from krnlai.core.memory.working_memory import WorkingMemory
from krnlai.core.models.cognitive import (
    CognitiveState,
    CycleEvent,
    CyclePhase,
    CycleStep,
)
from krnlai.core.models.envelope import (
    CommandEnvelope,
    CommandType,
    ResultEnvelope,
    ResultStatus,
)
from krnlai.core.models.thought import (
    ThoughtCategory,
    ThoughtClassification,
    ThoughtHorizon,
    ThoughtTrigger,
)
from krnlai.core.policies.engine import PolicyEngine
from krnlai.core.risk.scorer import RiskScorer
from krnlai.core.safety.rules import SafetyChecker
from krnlai.core.steps.attention import EnhancedAttentionStep
from krnlai.core.steps.execution import ExecutionStep
from krnlai.core.steps.learning import LearningStep
from krnlai.core.steps.metacognition import EnhancedMetacognitionStep
from krnlai.core.steps.moment_classifier import MomentClassifierStep
from krnlai.core.steps.planning import DynamicPlanningStep
from krnlai.core.stores.moment_store import MomentStore


@dataclass
class CycleConfig:
    max_iterations: int = 10
    step_timeout_ms: float = 5000
    cycle_timeout_ms: float = 30000
    safety_level: str = "strict"
    enable_emotions: bool = True
    enable_learning: bool = True


class StepContext:
    def __init__(self) -> None:
        self.data: Dict[str, Any] = {}
        self.errors: List[str] = []


class CognitiveCycleRunner:
    def __init__(
        self,
        config: Optional[CycleConfig] = None,
        safety_checker: Optional[SafetyChecker] = None,
        risk_scorer: Optional[RiskScorer] = None,
        vad_model: Optional[VADModel] = None,
        pain_reward: Optional[PainRewardModel] = None,
        working_memory: Optional[WorkingMemory] = None,
        episodic_memory: Optional[EpisodicMemory] = None,
        semantic_memory: Optional[SemanticMemory] = None,
        policy_engine: Optional[PolicyEngine] = None,
        attention_step: Optional[EnhancedAttentionStep] = None,
        metacognition_step: Optional[EnhancedMetacognitionStep] = None,
        moment_classifier: Optional[MomentClassifierStep] = None,
        moment_store: Optional[MomentStore] = None,
        planning_step: Optional[DynamicPlanningStep] = None,
        execution_step: Optional[ExecutionStep] = None,
        confidence_calibrator: Optional[ConfidenceCalibrator] = None,
        thought_graph: Optional[ThoughtGraph] = None,
        quality_assessor: Optional[ReasoningQualityAssessor] = None,
        adaptive_processor: Optional[AdaptiveProcessor] = None,
    ) -> None:
        self.config = config or CycleConfig()
        self.safety = safety_checker or SafetyChecker()
        self.risk_scorer = risk_scorer or RiskScorer()
        self.vad = vad_model or VADModel()
        self.pain_reward = pain_reward or PainRewardModel()
        self.working_memory = working_memory or WorkingMemory()
        self.episodic_memory = episodic_memory or EpisodicMemory()
        self.semantic_memory = semantic_memory or SemanticMemory()
        self.policies = policy_engine or PolicyEngine()
        self.attention_step = attention_step or EnhancedAttentionStep()
        self.metacognition_step = metacognition_step or EnhancedMetacognitionStep()
        self.moment_classifier = moment_classifier or MomentClassifierStep()
        self.moment_store = moment_store or MomentStore()
        self.planning_step = planning_step or DynamicPlanningStep()
        self.execution_step = execution_step or ExecutionStep()
        self.confidence_calibrator = confidence_calibrator or ConfidenceCalibrator()
        self.thought_graph = thought_graph or ThoughtGraph()
        self.quality_assessor = quality_assessor or ReasoningQualityAssessor()
        self.adaptive_processor = adaptive_processor or AdaptiveProcessor(
            default_max_iterations=self.config.max_iterations,
            default_safety_level=self.config.safety_level,
        )

        self.metacognition_step.confidence_calibrator = self.confidence_calibrator

        self._step_handlers: Dict[CycleStep, Callable[[StepContext], Any]] = {}
        self._on_event: List[Callable[[CycleEvent], None]] = []
        self._register_default_steps()

    def _register_default_steps(self) -> None:
        self._step_handlers = {
            CycleStep.SENSOR: self._step_sensor,
            CycleStep.ATTENTION: self._step_attention,
            CycleStep.MEMORY: self._step_memory,
            CycleStep.EVALUATION: self._step_evaluation,
            CycleStep.METACOGNITION: self._step_metacognition,
            CycleStep.PLANNING: self._step_planning,
            CycleStep.GOVERNANCE: self._step_governance,
            CycleStep.EXECUTION: self._step_execution,
            CycleStep.OUTCOME: self._step_outcome,
            CycleStep.LEARNING: self._step_learning,
        }

    def on_event(self, handler: Callable[[CycleEvent], None]) -> None:
        self._on_event.append(handler)

    def _emit(self, event: CycleEvent) -> None:
        for handler in self._on_event:
            handler(event)

    def _record_thought_node(
        self,
        state: CognitiveState,
        step: CycleStep,
        classification: ThoughtClassification,
        ctx: StepContext,
    ) -> None:
        moment_cat = ctx.data.get("moment_category")
        payload = state.command or ""
        node = ThoughtNode(
            cycle_id=state.cycle_id,
            step=step,
            category=classification.category,
            moment_category=moment_cat,
            content_hash=ThoughtGraph.compute_content_hash(payload),
            classification=classification,
            summary=f"{step.value}: {payload[:60]}" if payload else step.value,
        )
        self.thought_graph.add_node(node)

        if len(self.thought_graph._access_order) > 1:
            prev_id = None
            for nid in reversed(self.thought_graph._access_order[:-1]):
                prev = self.thought_graph.get_node(nid)
                if prev and prev.cycle_id == state.cycle_id:
                    prev_id = prev.id
                    break
            if prev_id:
                self.thought_graph.add_edge(prev_id, node.id, ThoughtRelation.SEQUENCES)

    async def run(self, command: str, context: Optional[Dict[str, Any]] = None) -> ResultEnvelope:
        cmd = CommandEnvelope(type=CommandType.TEXT, payload=command, context=context or {})
        return await self.run_command(cmd)

    async def run_command(self, command: CommandEnvelope) -> ResultEnvelope:
        start_time = time.monotonic()
        state = CognitiveState(
            command=command.payload,
            context=dict(command.context),
        )
        step_context = StepContext()

        for iteration in range(self.config.max_iterations):
            state.iteration = iteration
            if iteration > 0:
                state.phase = self._determine_phase(iteration)

            for step in CycleStep:
                state.current_step = step
                step_start = time.monotonic()

                classification = self._classify_step(step, step_context, state)
                event = CycleEvent(
                    cycle_id=state.cycle_id,
                    step=step,
                    status="running",
                    classification=classification,
                )

                try:
                    handler = self._step_handlers.get(step)
                    if handler:
                        result = await asyncio.wait_for(
                            self._run_handler(handler, step_context, command, state),
                            timeout=self.config.step_timeout_ms / 1000,
                        )
                        event.data = {"result": str(result)[:200]}
                except asyncio.TimeoutError:
                    event.status = "timeout"
                    step_context.errors.append(f"{step.value}: timeout")
                except Exception as e:
                    event.status = "error"
                    step_context.errors.append(f"{step.value}: {e}")

                event.duration_ms = (time.monotonic() - step_start) * 1000
                event.timestamp = datetime.now(timezone.utc)
                self._emit(event)

                if classification and event.status != "timeout":
                    self._record_thought_node(state, step, classification, step_context)

                if event.status == "error" and self.config.safety_level == "strict":
                    break

            if self._should_stop(state, step_context):
                break

        reasoning = self.quality_assessor.assess(
            input_text=command.payload,
            output_text=str(step_context.data.get("output", "")),
            context=step_context.data,
            thought_graph=self.thought_graph,
        )

        mode = self.adaptive_processor.determine_mode(
            reasoning_quality=reasoning.quality,
            bias_count=len(step_context.data.get("biases_detected", [])),
            calibration_error=self.confidence_calibrator.get_calibration_error("general"),
            cognitive_load=step_context.data.get("cognitive_load", 0.0),
            fatigue=step_context.data.get("fatigue", 0.0),
        )

        self.adaptive_processor.record_assessment(
            iteration=state.iteration,
            quality=reasoning.quality,
            coherence=reasoning.coherence,
            completeness=reasoning.completeness,
            soundness=reasoning.soundness,
            issues=reasoning.issues,
        )

        self.adaptive_processor.apply_mode_to_config(self.config)

        step_context.data["reasoning_quality"] = reasoning.quality
        step_context.data["reasoning_assessment"] = reasoning
        step_context.data["processing_mode"] = mode.mode

        elapsed_ms = (time.monotonic() - start_time) * 1000
        state.completed_at = datetime.now(timezone.utc)

        result = ResultEnvelope(
            command_id=command.id,
            status=ResultStatus.ERROR if step_context.errors else ResultStatus.SUCCESS,
            output=str(step_context.data.get("output", "")),
            error="; ".join(step_context.errors) if step_context.errors else None,
            risk_score=self.risk_scorer.evaluate(command.context),
            emotional_delta=self.vad.current.to_dict() if self.config.enable_emotions else None,
            duration_ms=elapsed_ms,
        )
        return result

    def stream_cycle(self, command: CommandEnvelope) -> AsyncGenerator[CycleEvent, None]:
        async def _stream() -> AsyncGenerator[CycleEvent, None]:
            events: List[CycleEvent] = []
            original_handler = self._on_event.copy()

            def capture(event: CycleEvent) -> None:
                events.append(event)

            self._on_event.append(capture)
            try:
                task = asyncio.create_task(self.run_command(command))
                while not task.done():
                    while events:
                        yield events.pop(0)
                    await asyncio.sleep(0.01)
                while events:
                    yield events.pop(0)
                await task
            finally:
                self._on_event = original_handler

        return _stream()

    async def _run_handler(
        self,
        handler: Callable,
        ctx: StepContext,
        cmd: CommandEnvelope,
        state: CognitiveState,
    ) -> Any:
        if asyncio.iscoroutinefunction(handler):
            return await handler(ctx, cmd, state)
        return handler(ctx, cmd, state)

    async def _step_sensor(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        ctx.data["input"] = cmd.payload
        ctx.data["context"] = cmd.context
        return f"Sensed input: {cmd.payload[:50]}..."

    async def _step_attention(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        result = await self.attention_step.execute(cmd, state, ctx.data)
        ctx.data.update(result)
        return f"Intent: {result['intent']}, Topic: {result['topic']}, Complexity: {result['complexity']}"

    async def _step_memory(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        recall = self.episodic_memory.recent(3)
        semantic = self.semantic_memory.search(cmd.payload)
        wm_id = self.working_memory.store(cmd.payload)
        ctx.data["recalled_episodes"] = len(recall)
        ctx.data["recalled_facts"] = len(semantic)
        ctx.data["working_memory_id"] = str(wm_id)
        return f"Recalled {len(recall)} episodes, {len(semantic)} facts"

    async def _step_evaluation(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        safety_context = {
            "action": "kernel.handle",
            "payload": cmd.payload,
            "context_id": str(state.cycle_id),
        }
        verdict = self.safety.evaluate_all(safety_context)
        risk = self.risk_scorer.evaluate(cmd.context)
        ctx.data["safety_verdict"] = verdict
        ctx.data["risk_score"] = risk
        state.risk_score = risk

        if self.config.enable_emotions:
            neg_bias = -0.1 if risk > 0.5 else 0.05
            self.vad.update(
                delta_valence=-0.2 if not verdict.allowed else neg_bias,
                delta_arousal=risk * 0.3,
                trigger="evaluation",
            )

        moment_result = await self.moment_classifier.execute(cmd, state, ctx.data)
        ctx.data.update(moment_result)
        snapshot = self.moment_classifier.moment_history[-1]
        self.moment_store.add(snapshot.moment_id, snapshot)

        if not verdict.allowed:
            return f"BLOCKED: {verdict.reason}"
        return f"Risk: {risk:.2f}, Allowed: {verdict.allowed}"

    async def _step_metacognition(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        context = dict(ctx.data)
        context["risk_score"] = ctx.data.get("risk_score", 0.0)
        context["novelty"] = ctx.data.get("novelty", 0.0)
        context["fatigue"] = ctx.data.get("fatigue", 0.0)
        context["confidence"] = ctx.data.get("confidence", 0.5)
        context["urgency"] = ctx.data.get("urgency", 0.0)
        context["complexity"] = ctx.data.get("complexity", 0.0)
        context["emotional_state"] = state.emotional_state
        context["moment_category"] = ctx.data.get("moment_category")
        context["moment_cognitive_load"] = ctx.data.get("moment_cognitive_load", 0.0)
        context["moment_confidence"] = ctx.data.get("moment_confidence", 0.0)
        result = await self.metacognition_step.execute(cmd, state, context)
        ctx.data.update(result)
        quality = result['reasoning_quality']
        return f"Quality: {quality}, Biases: {len(result['biases_detected'])}, Load: {result['cognitive_load']}"

    async def _step_planning(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        plan_context = dict(ctx.data)
        plan_context.setdefault("complexity", 0.3)
        plan_context.setdefault("thought_type", "analytical")
        plan_context.setdefault("cognitive_load", ctx.data.get("moment_cognitive_load", 0.0))
        plan_context.setdefault("risk_score", ctx.data.get("risk_score", 0.0))
        plan_context.setdefault("metacognitive_observations", ctx.data.get("observations", []))
        result = await self.planning_step.execute(cmd, state, plan_context)
        ctx.data["plan"] = result["plan"]
        ctx.data["current_plan_step"] = result["current_step_index"]
        ctx.data["decomposition_strategy"] = result["decomposition_strategy"]
        ctx.data["max_plan_steps"] = self.planning_step._max_plan_steps
        return f"Plan ({result['decomposition_strategy']}): {result['plan']}"

    async def _step_governance(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        policy_context = {
            "action": "kernel.handle",
            "payload": cmd.payload,
            "risk_score": ctx.data.get("risk_score", 0.0),
        }
        result = self.policies.execute(policy_context)
        ctx.data["policy_result"] = result
        return "Policy check passed" if result.get("allowed", True) else "Policy check failed"

    async def _step_execution(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        result = await self.execution_step.execute(cmd, state, ctx.data)
        ctx.data["output"] = result["output"]
        ctx.data["steps_executed"] = result["steps_executed"]
        ctx.data["current_plan_step"] = ctx.data.get("current_plan_step", 0) + len(result["steps_executed"])
        if self.config.enable_emotions:
            self.vad.update(delta_valence=0.05, trigger="execution_complete")
        return result["output"]

    async def _step_outcome(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        output = ctx.data.get("output", "")
        episode_id = self.episodic_memory.remember(
            content={"input": cmd.payload, "output": output},
            episode_type="cycle",
        )
        ctx.data["episode_id"] = episode_id
        return f"Outcome recorded: {str(episode_id)[:8]}..."

    async def _step_learning(self, ctx: StepContext, cmd: CommandEnvelope, state: CognitiveState) -> str:
        if not self.config.enable_learning:
            return "Learning disabled"
        learning_step = LearningStep(
            semantic_memory=self.semantic_memory,
            vad_model=self.vad,
            pain_reward=self.pain_reward,
            confidence_calibrator=self.confidence_calibrator,
        )
        result = await learning_step.execute(cmd, state, ctx.data)
        return "Learning complete" if result.get("learning_status") == "completed" else "Learning disabled"

    def _determine_phase(self, iteration: int) -> CyclePhase:
        if iteration < 2:
            return CyclePhase.PERCEPTION
        elif iteration < 5:
            return CyclePhase.DELIBERATION
        elif iteration < 8:
            return CyclePhase.ACTION
        return CyclePhase.REFLECTION

    @staticmethod
    def _classify_step(step: CycleStep, ctx: StepContext, state: CognitiveState) -> ThoughtClassification:
        category_map = {
            CycleStep.SENSOR: ThoughtCategory.SENSORY_INPUT,
            CycleStep.ATTENTION: ThoughtCategory.PATTERN_MATCH,
            CycleStep.MEMORY: ThoughtCategory.EPISODIC_RECALL,
            CycleStep.EVALUATION: ThoughtCategory.EVALUATIVE,
            CycleStep.METACOGNITION: ThoughtCategory.SELF_OBSERVATION,
            CycleStep.PLANNING: ThoughtCategory.SEQUENCE_PLANNING,
            CycleStep.GOVERNANCE: ThoughtCategory.POLICY_CHECK,
            CycleStep.EXECUTION: ThoughtCategory.COMMUNICATION,
            CycleStep.OUTCOME: ThoughtCategory.EVALUATIVE,
            CycleStep.LEARNING: ThoughtCategory.SEMANTIC_RECALL,
        }
        category = category_map.get(step, ThoughtCategory.CONTEXT_AWARE)

        risk = ctx.data.get("risk_score", 0.0)
        complexity = min(1.0, max(0.0, risk + 0.3))
        abstractness = 0.3 if step in (CycleStep.METACOGNITION, CycleStep.PLANNING) else 0.1
        novelty = min(1.0, max(0.0, risk * 0.8 + 0.1))
        confidence = 1.0 - risk * 0.5
        valence = ctx.data.get("valence_delta", 0.0)
        arousal = risk * 0.5

        trigger_map = {
            CycleStep.SENSOR: ThoughtTrigger.EXTERNAL,
            CycleStep.ATTENTION: ThoughtTrigger.EXTERNAL,
            CycleStep.MEMORY: ThoughtTrigger.MEMORY,
            CycleStep.LEARNING: ThoughtTrigger.INFERENCE,
        }
        trigger = trigger_map.get(step, ThoughtTrigger.INTERNAL)

        horizon_map = {
            CycleStep.PLANNING: ThoughtHorizon.NEAR_FUTURE,
            CycleStep.LEARNING: ThoughtHorizon.FAR_FUTURE,
            CycleStep.MEMORY: ThoughtHorizon.PAST,
        }
        horizon = horizon_map.get(step, ThoughtHorizon.PRESENT)

        return ThoughtClassification(
            category=category,
            subcategory=None,
            complexity=complexity,
            abstractness=abstractness,
            novelty=novelty,
            confidence=confidence,
            horizon=horizon,
            duration_ms=0.0,
            valence_delta=valence,
            arousal_delta=arousal,
            trigger=trigger,
            antecedents=[],
        )

    def _should_stop(self, state: CognitiveState, ctx: StepContext) -> bool:
        if ctx.errors:
            return True
        if state.iteration >= self.config.max_iterations - 1:
            return True
        return False
