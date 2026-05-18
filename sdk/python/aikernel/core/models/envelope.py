from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class CommandType(str, Enum):
    TEXT = "text"
    TOOL = "tool"
    QUERY = "query"
    COMMAND = "command"
    SYSTEM = "system"


class CommandEnvelope(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    type: CommandType = CommandType.TEXT
    payload: str
    metadata: Dict[str, Any] = Field(default_factory=dict)
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    source: Optional[str] = None
    context: Dict[str, Any] = Field(default_factory=dict)

    def model_dump_json_compat(self) -> str:
        return self.model_dump_json()


class ResultStatus(str, Enum):
    SUCCESS = "success"
    ERROR = "error"
    BLOCKED = "blocked"
    PENDING = "pending"
    CANCELLED = "cancelled"


class ResultEnvelope(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    command_id: UUID
    status: ResultStatus = ResultStatus.SUCCESS
    output: str = ""
    error: Optional[str] = None
    safety_verdict: Optional[SafetyVerdict] = None
    risk_score: float = 0.0
    emotional_delta: Optional[Dict[str, float]] = None
    steps_executed: List[str] = Field(default_factory=list)
    duration_ms: float = 0.0
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    metadata: Dict[str, Any] = Field(default_factory=dict)

    def model_dump_json_compat(self) -> str:
        return self.model_dump_json()


from krnlai.core.models.safety import SafetyVerdict
