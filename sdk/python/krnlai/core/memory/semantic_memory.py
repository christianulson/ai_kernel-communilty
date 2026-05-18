from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional, Tuple
from uuid import UUID, uuid4


@dataclass
class SemanticFact:
    id: UUID = field(default_factory=uuid4)
    subject: str = ""
    predicate: str = ""
    object_val: str = ""
    confidence: float = 1.0
    embedding: Optional[List[float]] = None
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    metadata: Dict[str, Any] = field(default_factory=dict)

    def to_triple(self) -> Tuple[str, str, str]:
        return (self.subject, self.predicate, self.object_val)


class SemanticMemory:
    def __init__(self) -> None:
        self._facts: List[SemanticFact] = []

    def store_fact(
        self,
        subject: str,
        predicate: str,
        object_val: str,
        confidence: float = 1.0,
    ) -> UUID:
        fact = SemanticFact(
            subject=subject,
            predicate=predicate,
            object_val=object_val,
            confidence=confidence,
        )
        self._facts.append(fact)
        return fact.id

    def query(self, subject: Optional[str] = None, predicate: Optional[str] = None) -> List[SemanticFact]:
        results = self._facts
        if subject:
            results = [f for f in results if subject.lower() in f.subject.lower()]
        if predicate:
            results = [f for f in results if predicate.lower() in f.predicate.lower()]
        return sorted(results, key=lambda f: f.confidence, reverse=True)

    def search(self, text: str) -> List[SemanticFact]:
        text_lower = text.lower()
        return [
            f for f in self._facts
            if text_lower in f.subject.lower()
            or text_lower in f.predicate.lower()
            or text_lower in f.object_val.lower()
        ]

    def clear(self) -> None:
        self._facts.clear()

    @property
    def count(self) -> int:
        return len(self._facts)

    @property
    def all_facts(self) -> List[SemanticFact]:
        return list(self._facts)
