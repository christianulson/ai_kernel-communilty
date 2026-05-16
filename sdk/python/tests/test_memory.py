from __future__ import annotations

import pytest

from aikernel.core.memory.working_memory import WorkingMemory
from aikernel.core.memory.episodic_memory import EpisodicMemory
from aikernel.core.memory.semantic_memory import SemanticMemory
from aikernel.core.memory.emotional_memory import EmotionalMemory
from aikernel.core.emotion.vad import VADState


class TestWorkingMemory:
    def test_Store_SimpleValue_ShouldReturnId(self):
        wm = WorkingMemory(capacity=7)
        item_id = wm.store("hello")
        assert item_id is not None

    def test_Recall_Existing_ShouldReturnContent(self):
        wm = WorkingMemory()
        item_id = wm.store("test content")
        recalled = wm.recall(item_id)
        assert recalled == "test content"

    def test_Recall_NonExisting_ShouldReturnNone(self):
        wm = WorkingMemory()
        import uuid
        result = wm.recall(uuid.uuid4())
        assert result is None

    def test_Capacity_Exceeded_ShouldEvictOldest(self):
        wm = WorkingMemory(capacity=2)
        id1 = wm.store("first")
        wm.store("second")
        wm.store("third")
        assert wm.recall(id1) is None
        assert wm.count == 2

    def test_Search_ByKeyword_ShouldFind(self):
        wm = WorkingMemory()
        wm.store("the quick brown fox")
        wm.store("lazy dog")
        results = wm.search("fox")
        assert len(results) == 1

    def test_Clear_ShouldEmpty(self):
        wm = WorkingMemory()
        wm.store("test")
        wm.clear()
        assert wm.count == 0

    def test_Contents_ShouldReturnAll(self):
        wm = WorkingMemory()
        wm.store("a")
        wm.store("b")
        assert len(wm.contents) == 2


class TestEpisodicMemory:
    def test_Remember_ShouldReturnId(self):
        em = EpisodicMemory()
        eid = em.remember("test event")
        assert eid is not None

    def test_Recall_Existing_ShouldReturnEntry(self):
        em = EpisodicMemory()
        eid = em.remember({"text": "hello"}, episode_type="chat")
        entry = em.recall(eid)
        assert entry is not None
        assert entry.episode_type == "chat"
        assert entry.content["text"] == "hello"

    def test_Recall_NonExisting_ShouldReturnNone(self):
        em = EpisodicMemory()
        import uuid
        entry = em.recall(uuid.uuid4())
        assert entry is None

    def test_Search_ByKeyword_ShouldFind(self):
        em = EpisodicMemory()
        em.remember("important meeting notes")
        results = em.search("meeting")
        assert len(results) == 1

    def test_Recent_ShouldReturnOrdered(self):
        em = EpisodicMemory()
        em.remember("first")
        em.remember("second")
        em.remember("third")
        recent = em.recent(2)
        assert len(recent) == 2

    def test_MaxEntries_ShouldPrune(self):
        em = EpisodicMemory(max_entries=3)
        em.remember("a")
        em.remember("b")
        em.remember("c")
        em.remember("d")
        assert em.count <= 3

    def test_Clear_ShouldEmpty(self):
        em = EpisodicMemory()
        em.remember("test")
        em.clear()
        assert em.count == 0


class TestSemanticMemory:
    def test_StoreFact_ShouldReturnId(self):
        sm = SemanticMemory()
        fid = sm.store_fact("agent", "can", "think")
        assert fid is not None

    def test_Query_BySubject_ShouldReturn(self):
        sm = SemanticMemory()
        sm.store_fact("cat", "is", "animal")
        sm.store_fact("dog", "is", "animal")
        results = sm.query(subject="cat")
        assert len(results) == 1

    def test_Query_ByPredicate_ShouldReturn(self):
        sm = SemanticMemory()
        sm.store_fact("sky", "color", "blue")
        results = sm.query(predicate="color")
        assert len(results) == 1

    def test_Search_ByText_ShouldFind(self):
        sm = SemanticMemory()
        sm.store_fact("earth", "orbits", "sun")
        results = sm.search("sun")
        assert len(results) == 1

    def test_AllFacts_ShouldReturnAll(self):
        sm = SemanticMemory()
        sm.store_fact("a", "is", "1")
        sm.store_fact("b", "is", "2")
        assert len(sm.all_facts) == 2

    def test_Clear_ShouldEmpty(self):
        sm = SemanticMemory()
        sm.store_fact("x", "y", "z")
        sm.clear()
        assert sm.count == 0


class TestEmotionalMemory:
    def test_Record_ShouldReturnId(self):
        em = EmotionalMemory()
        eid = em.record(VADState(valence=0.5))
        assert eid is not None

    def test_Recent_ShouldReturnOrdered(self):
        em = EmotionalMemory()
        em.record(VADState(valence=0.1), "first")
        em.record(VADState(valence=0.2), "second")
        em.record(VADState(valence=0.3), "third")
        recent = em.recent(2)
        assert len(recent) == 2
        assert recent[0].trigger == "third"

    def test_Timeline_ShouldBeChronological(self):
        em = EmotionalMemory()
        em.record(VADState(valence=0.1), "first")
        em.record(VADState(valence=0.2), "second")
        timeline = em.timeline()
        assert len(timeline) == 2

    def test_SearchByTrigger_ShouldMatch(self):
        em = EmotionalMemory()
        em.record(VADState(), "error occurred")
        em.record(VADState(), "success")
        results = em.search_by_trigger("error")
        assert len(results) == 1

    def test_Clear_ShouldEmpty(self):
        em = EmotionalMemory()
        em.record(VADState())
        em.clear()
        assert em.count == 0
