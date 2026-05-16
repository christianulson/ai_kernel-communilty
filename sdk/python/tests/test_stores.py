from __future__ import annotations

import threading
from uuid import UUID, uuid4

import pytest

from aikernel.core.stores.inmemory import InMemoryStore
from aikernel.core.stores.exceptions import ItemNotFoundError


class TestInMemoryStore:
    def test_Add_Get_ShouldReturnSame(self):
        store = InMemoryStore()
        item_id = uuid4()
        store.add(item_id, "test value")
        assert store.get(item_id) == "test value"

    def test_Get_NonExisting_ShouldRaise(self):
        store = InMemoryStore()
        with pytest.raises(ItemNotFoundError):
            store.get(uuid4())

    def test_Update_Existing_ShouldModify(self):
        store = InMemoryStore()
        item_id = uuid4()
        store.add(item_id, "original")
        store.update(item_id, "modified")
        assert store.get(item_id) == "modified"

    def test_Update_NonExisting_ShouldRaise(self):
        store = InMemoryStore()
        with pytest.raises(ItemNotFoundError):
            store.update(uuid4(), "value")

    def test_Delete_Existing_ShouldRemove(self):
        store = InMemoryStore()
        item_id = uuid4()
        store.add(item_id, "value")
        assert store.delete(item_id) is True
        with pytest.raises(ItemNotFoundError):
            store.get(item_id)

    def test_Delete_NonExisting_ShouldReturnFalse(self):
        store = InMemoryStore()
        assert store.delete(uuid4()) is False

    def test_Find_ShouldMatchPredicate(self):
        store = InMemoryStore[str]()
        id1 = uuid4()
        id2 = uuid4()
        store.add(id1, "apple")
        store.add(id2, "banana")
        results = store.find(lambda x: "a" in x)
        assert len(results) == 2

    def test_All_ShouldReturnAll(self):
        store = InMemoryStore()
        store.add(uuid4(), "a")
        store.add(uuid4(), "b")
        assert len(store.all()) == 2

    def test_Count_ShouldBeAccurate(self):
        store = InMemoryStore()
        assert store.count() == 0
        store.add(uuid4(), "x")
        assert store.count() == 1

    def test_Contains_ShouldWork(self):
        store = InMemoryStore()
        item_id = uuid4()
        assert (item_id in store) is False
        store.add(item_id, "value")
        assert (item_id in store) is True

    def test_Clear_ShouldEmpty(self):
        store = InMemoryStore()
        store.add(uuid4(), "a")
        store.add(uuid4(), "b")
        store.clear()
        assert store.count() == 0

    def test_ThreadSafety_ConcurrentAccess(self):
        store = InMemoryStore()
        errors = []

        def add_items():
            for _ in range(100):
                try:
                    store.add(uuid4(), "value")
                except Exception as e:
                    errors.append(e)

        threads = [threading.Thread(target=add_items) for _ in range(10)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()

        assert len(errors) == 0
        assert store.count() == 1000
