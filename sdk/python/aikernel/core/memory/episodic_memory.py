from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from aikernel.core.stores.exceptions import ItemNotFoundError
from aikernel.core.stores.inmemory import InMemoryStore


class EpisodicMemoryEntry:
    def __init__(
        self,
        content: Any,
        episode_type: str = "general",
        metadata: Optional[Dict[str, Any]] = None,
    ) -> None:
        self.id = uuid4()
        self.content = content
        self.episode_type = episode_type
        self.timestamp = datetime.now(timezone.utc)
        self.metadata = metadata or {}

    def to_dict(self) -> Dict[str, Any]:
        return {
            "id": str(self.id),
            "content": self.content,
            "episode_type": self.episode_type,
            "timestamp": self.timestamp.isoformat(),
            "metadata": self.metadata,
        }


class EpisodicMemory:
    def __init__(self, max_entries: int = 1000) -> None:
        self._store: InMemoryStore[EpisodicMemoryEntry] = InMemoryStore()
        self._max_entries = max_entries

    def remember(self, content: Any, episode_type: str = "general") -> UUID:
        self._prune_if_needed()
        entry = EpisodicMemoryEntry(content=content, episode_type=episode_type)
        self._store.add(entry.id, entry)
        return entry.id

    def recall(self, entry_id: UUID) -> Optional[EpisodicMemoryEntry]:
        try:
            return self._store.get(entry_id)
        except (KeyError, ItemNotFoundError):
            return None

    def search(self, query: str) -> List[EpisodicMemoryEntry]:
        query_lower = query.lower()
        return self._store.find(
            lambda e: (
                query_lower in str(e.content).lower()
                or query_lower in e.episode_type.lower()
            )
        )

    def recent(self, count: int = 10) -> List[EpisodicMemoryEntry]:
        all_entries = self._store.all()
        all_entries.sort(key=lambda e: e.timestamp, reverse=True)
        return all_entries[:count]

    def clear(self) -> None:
        self._store.clear()

    @property
    def count(self) -> int:
        return self._store.count()

    def _prune_if_needed(self) -> None:
        while self._store.count() >= self._max_entries:
            oldest = min(self._store.all(), key=lambda e: e.timestamp)
            self._store.delete(oldest.id)
