from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class RiskLevel(str, Enum):
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"
    CRITICAL = "critical"


class RuleSeverity(str, Enum):
    ERROR = "error"
    WARNING = "warning"
    INFO = "info"


class RuleVerdict(BaseModel):
    rule_id: str
    rule_name: str
    passed: bool
    severity: RuleSeverity = RuleSeverity.ERROR
    message: str = ""
    details: Dict[str, Any] = Field(default_factory=dict)


class SafetyVerdict(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    allowed: bool = True
    risk_level: RiskLevel = RiskLevel.LOW
    risk_score: float = 0.0
    rule_results: List[RuleVerdict] = Field(default_factory=list)
    blocked_by: List[str] = Field(default_factory=list)
    requires_approval: bool = False
    reason: str = ""
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    metadata: Dict[str, Any] = Field(default_factory=dict)
