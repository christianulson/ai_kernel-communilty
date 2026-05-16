from __future__ import annotations

import threading
from typing import Callable, Dict, Generic, List, TypeVar
from uuid import UUID

from aikernel.core.stores.exceptions import ItemNotFoundError

T = TypeVar("T")


class InMemoryStore(Generic[T]):
    def __init__(self) -> None:
        self._items: Dict[UUID, T] = {}
        self._lock = threading.RLock()

    def add(self, item_id: UUID, item: T) -> T:
        with self._lock:
            self._items[item_id] = item
            return item

    def get(self, item_id: UUID) -> T:
        with self._lock:
            if item_id not in self._items:
                raise ItemNotFoundError(f"Item {item_id} not found")
            return self._items[item_id]

    def update(self, item_id: UUID, item: T) -> T:
        with self._lock:
            if item_id not in self._items:
                raise ItemNotFoundError(f"Item {item_id} not found")
            self._items[item_id] = item
            return item

    def delete(self, item_id: UUID) -> bool:
        with self._lock:
            if item_id not in self._items:
                return False
            del self._items[item_id]
            return True

    def find(self, predicate: Callable[[T], bool]) -> List[T]:
        with self._lock:
            return [item for item in self._items.values() if predicate(item)]

    def all(self) -> List[T]:
        with self._lock:
            return list(self._items.values())

    def clear(self) -> None:
        with self._lock:
            self._items.clear()

    def count(self) -> int:
        with self._lock:
            return len(self._items)

    def __contains__(self, item_id: UUID) -> bool:
        with self._lock:
            return item_id in self._items
