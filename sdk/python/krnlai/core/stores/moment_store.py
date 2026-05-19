from __future__ import annotations

from typing import List

from krnlai.core.models.moment import MomentCategory, MomentSnapshot
from krnlai.core.stores.inmemory import InMemoryStore


class MomentStore(InMemoryStore[MomentSnapshot]):
    def list_recent(self, take: int = 10) -> List[MomentSnapshot]:
        all_items = self.all()
        sorted_items = sorted(all_items, key=lambda x: x.timestamp, reverse=True)
        return sorted_items[:take]

    def filter_by_category(self, category: MomentCategory) -> List[MomentSnapshot]:
        return self.find(lambda m: m.category == category)
