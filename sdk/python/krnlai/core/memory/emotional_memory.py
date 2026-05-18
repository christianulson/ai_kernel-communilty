from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from krnlai.core.emotion.vad import VADState


class EmotionalMemoryEntry:
    def __init__(
        self,
        state: VADState,
        trigger: str = "",
        context: Optional[Dict[str, Any]] = None,
    ) -> None:
        self.id = uuid4()
        self.state = state
        self.trigger = trigger
        self.timestamp = datetime.now(timezone.utc)
        self.context = context or {}

    def to_dict(self) -> Dict[str, Any]:
        return {
            "id": str(self.id),
            "state": self.state.to_dict(),
            "trigger": self.trigger,
            "timestamp": self.timestamp.isoformat(),
            "context": self.context,
        }


class EmotionalMemory:
    def __init__(self) -> None:
        self._entries: List[EmotionalMemoryEntry] = []
        self._order_counter: int = 0

    def record(self, state: VADState, trigger: str = "") -> UUID:
        self._order_counter += 1
        entry = EmotionalMemoryEntry(state=state, trigger=trigger)
        entry._order = self._order_counter
        self._entries.append(entry)
        return entry.id

    def recent(self, count: int = 10) -> List[EmotionalMemoryEntry]:
        sorted_entries = sorted(
            self._entries,
            key=lambda e: (e.timestamp, getattr(e, '_order', 0)),
            reverse=True,
        )
        return sorted_entries[:count]

    def timeline(self) -> List[EmotionalMemoryEntry]:
        return sorted(self._entries, key=lambda e: e.timestamp)

    def search_by_trigger(self, trigger_substring: str) -> List[EmotionalMemoryEntry]:
        trig_lower = trigger_substring.lower()
        return [e for e in self._entries if trig_lower in e.trigger.lower()]

    def clear(self) -> None:
        self._entries.clear()

    @property
    def count(self) -> int:
        return len(self._entries)
