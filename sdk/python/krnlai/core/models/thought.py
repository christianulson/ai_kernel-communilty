from __future__ import annotations

from enum import Enum
from typing import List, Optional
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class ThoughtCategory(str, Enum):
    # Perception
    SENSORY_INPUT = "sensory_input"
    PATTERN_MATCH = "pattern_match"
    CONTEXT_AWARE = "context_aware"

    # Memory
    EPISODIC_RECALL = "episodic_recall"
    SEMANTIC_RECALL = "semantic_recall"
    WORKING_MEMORY = "working_memory"
    EMOTIONAL_RECALL = "emotional_recall"
    PROCEDURAL_RECALL = "procedural_recall"

    # Reasoning
    DEDUCTIVE = "deductive"
    INDUCTIVE = "inductive"
    ABDUCTIVE = "abductive"
    ANALOGICAL = "analogical"
    CAUSAL = "causal"
    COUNTERFACTUAL = "counterfactual"

    # Decision
    EVALUATIVE = "evaluative"
    COMPARATIVE = "comparative"
    RISK_ASSESSMENT = "risk_assessment"
    POLICY_CHECK = "policy_check"

    # Planning
    GOAL_DECOMPOSITION = "goal_decomposition"
    SEQUENCE_PLANNING = "sequence_planning"
    CONTINGENCY_PLANNING = "contingency_planning"

    # Metacognition
    SELF_OBSERVATION = "self_observation"
    CONFIDENCE_ESTIMATION = "confidence_estimation"
    BIAS_DETECTION = "bias_detection"
    REASONING_QUALITY = "reasoning_quality"

    # Creative
    ANALOGY_FORMATION = "analogy_formation"
    HYPOTHESIS_GENERATION = "hypothesis_generation"
    INSIGHT = "insight"

    # Social
    THEORY_OF_MIND = "theory_of_mind"
    COMMUNICATION = "communication"


class ThoughtHorizon(str, Enum):
    PAST = "past"
    PRESENT = "present"
    NEAR_FUTURE = "near_future"
    FAR_FUTURE = "far_future"


class ThoughtTrigger(str, Enum):
    INTERNAL = "internal"
    EXTERNAL = "external"
    MEMORY = "memory"
    INFERENCE = "inference"


class ThoughtClassification(BaseModel):
    thought_id: UUID = Field(default_factory=uuid4)
    category: ThoughtCategory
    subcategory: Optional[str] = None
    complexity: float = Field(default=0.5, ge=0.0, le=1.0)
    abstractness: float = Field(default=0.5, ge=0.0, le=1.0)
    novelty: float = Field(default=0.5, ge=0.0, le=1.0)
    confidence: float = Field(default=0.5, ge=0.0, le=1.0)
    horizon: ThoughtHorizon = ThoughtHorizon.PRESENT
    duration_ms: float = 0.0
    valence_delta: float = 0.0
    arousal_delta: float = 0.0
    trigger: ThoughtTrigger = ThoughtTrigger.INTERNAL
    antecedents: List[UUID] = Field(default_factory=list)
