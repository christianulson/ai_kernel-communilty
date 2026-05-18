from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4


@dataclass
class WorkingMemorySlot:
    id: UUID = field(default_factory=uuid4)
    content: Any = None
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    ttl_seconds: float = 60.0
    metadata: Dict[str, Any] = field(default_factory=dict)

    @property
    def is_expired(self) -> bool:
        elapsed = (datetime.now(timezone.utc) - self.timestamp).total_seconds()
        return elapsed > self.ttl_seconds


class WorkingMemory:
    def __init__(self, capacity: int = 7) -> None:
        self._capacity = capacity
        self._slots: List[WorkingMemorySlot] = []

    def store(self, content: Any, ttl_seconds: float = 60.0) -> UUID:
        self._evict_expired()
        slot = WorkingMemorySlot(content=content, ttl_seconds=ttl_seconds)
        if len(self._slots) >= self._capacity:
            self._slots.pop(0)
        self._slots.append(slot)
        return slot.id

    def recall(self, slot_id: UUID) -> Optional[Any]:
        self._evict_expired()
        for slot in self._slots:
            if slot.id == slot_id:
                slot.timestamp = datetime.now(timezone.utc)
                return slot.content
        return None

    def search(self, query: str) -> List[Dict[str, Any]]:
        self._evict_expired()
        query_lower = query.lower()
        results = []
        for slot in self._slots:
            content_str = str(slot.content).lower() if slot.content else ""
            if query_lower in content_str:
                results.append({
                    "id": slot.id,
                    "content": slot.content,
                    "age_seconds": (datetime.now(timezone.utc) - slot.timestamp).total_seconds(),
                })
        return results

    def clear(self) -> None:
        self._slots.clear()

    @property
    def count(self) -> int:
        self._evict_expired()
        return len(self._slots)

    @property
    def capacity(self) -> int:
        return self._capacity

    @property
    def contents(self) -> List[Any]:
        self._evict_expired()
        return [slot.content for slot in self._slots]

    def _evict_expired(self) -> None:
        now = datetime.now(timezone.utc)
        self._slots = [
            slot for slot in self._slots
            if (now - slot.timestamp).total_seconds() <= slot.ttl_seconds
        ]
