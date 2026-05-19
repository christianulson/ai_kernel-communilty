from __future__ import annotations

from uuid import UUID, uuid4

import pytest

from krnlai.core.cognition.thought_graph import (
    ThoughtEdge,
    ThoughtGraph,
    ThoughtNode,
    ThoughtRelation,
)
from krnlai.core.models.cognitive import CycleStep
from krnlai.core.models.moment import MomentCategory
from krnlai.core.models.thought import ThoughtCategory


class TestThoughtNode:
    def test_Create_DefaultFields_ShouldHaveUUID(self):
        node = ThoughtNode()
        assert node.id is not None
        assert isinstance(node.id, UUID)

    def test_Create_WithAllFields_ShouldStore(self):
        nid = uuid4()
        cid = uuid4()
        node = ThoughtNode(
            id=nid,
            cycle_id=cid,
            step=CycleStep.EVALUATION,
            category=ThoughtCategory.EVALUATIVE,
            moment_category=MomentCategory.CONFLICT,
            summary="test thought",
        )
        assert node.id == nid
        assert node.cycle_id == cid
        assert node.step == CycleStep.EVALUATION
        assert node.category == ThoughtCategory.EVALUATIVE
        assert node.moment_category == MomentCategory.CONFLICT
        assert node.summary == "test thought"


class TestThoughtEdge:
    def test_Create_DefaultRelation_ShouldBeSequences(self):
        edge = ThoughtEdge(source_id=uuid4(), target_id=uuid4())
        assert edge.relation == ThoughtRelation.SEQUENCES
        assert edge.weight == 1.0

    def test_Create_WithRelation_ShouldStore(self):
        src = uuid4()
        tgt = uuid4()
        edge = ThoughtEdge(
            source_id=src,
            target_id=tgt,
            relation=ThoughtRelation.CONTRADICTS,
            weight=0.8,
        )
        assert edge.source_id == src
        assert edge.target_id == tgt
        assert edge.relation == ThoughtRelation.CONTRADICTS
        assert edge.weight == 0.8


class TestThoughtRelation:
    def test_Enum_AllMembers_ShouldBe12(self):
        assert len(ThoughtRelation) == 12


class TestThoughtGraph:
    def test_AddNode_ShouldIncreaseCount(self):
        graph = ThoughtGraph()
        graph.add_node(ThoughtNode())
        assert graph.node_count == 1

    def test_AddNode_Duplicate_ShouldNotIncrease(self):
        graph = ThoughtGraph()
        node = ThoughtNode()
        graph.add_node(node)
        graph.add_node(node)
        assert graph.node_count == 1

    def test_AddEdge_ShouldCreateEdge(self):
        graph = ThoughtGraph()
        a = ThoughtNode()
        b = ThoughtNode()
        graph.add_node(a)
        graph.add_node(b)
        graph.add_edge(a.id, b.id, ThoughtRelation.SEQUENCES)
        assert graph.edge_count == 1

    def test_AddEdge_MissingNodes_ShouldNotCreate(self):
        graph = ThoughtGraph()
        graph.add_edge(uuid4(), uuid4())
        assert graph.edge_count == 0

    def test_GetNode_Existing_ShouldReturn(self):
        graph = ThoughtGraph()
        node = ThoughtNode(summary="hello")
        graph.add_node(node)
        result = graph.get_node(node.id)
        assert result is not None
        assert result.summary == "hello"

    def test_GetNode_Missing_ShouldReturnNone(self):
        graph = ThoughtGraph()
        assert graph.get_node(uuid4()) is None

    def test_GetReasoningChain_SingleNode_ShouldReturnSelf(self):
        graph = ThoughtGraph()
        node = ThoughtNode()
        graph.add_node(node)
        chain = graph.get_reasoning_chain(node.id)
        assert len(chain) == 1
        assert chain[0].id == node.id

    def test_GetReasoningChain_Linear_ShouldReturnAll(self):
        graph = ThoughtGraph()
        a = ThoughtNode(summary="a")
        b = ThoughtNode(summary="b")
        c = ThoughtNode(summary="c")
        graph.add_node(a)
        graph.add_node(b)
        graph.add_node(c)
        graph.add_edge(a.id, b.id, ThoughtRelation.SEQUENCES)
        graph.add_edge(b.id, c.id, ThoughtRelation.SEQUENCES)
        chain = graph.get_reasoning_chain(c.id)
        assert len(chain) == 3

    def test_FindContradictions_NoEdges_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        assert graph.find_contradictions() == []

    def test_FindContradictions_WithContradicts_ShouldFind(self):
        graph = ThoughtGraph()
        a = ThoughtNode(summary="a")
        b = ThoughtNode(summary="b")
        graph.add_node(a)
        graph.add_node(b)
        graph.add_edge(a.id, b.id, ThoughtRelation.CONTRADICTS)
        pairs = graph.find_contradictions()
        assert len(pairs) == 1
        assert pairs[0][0].id == a.id
        assert pairs[0][1].id == b.id

    def test_FindLoops_NoCycles_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        a = ThoughtNode()
        b = ThoughtNode()
        c = ThoughtNode()
        graph.add_node(a)
        graph.add_node(b)
        graph.add_node(c)
        graph.add_edge(a.id, b.id)
        graph.add_edge(b.id, c.id)
        assert graph.find_loops() == []

    def test_FindLoops_WithCycle_ShouldDetect(self):
        graph = ThoughtGraph()
        a = ThoughtNode(summary="a")
        b = ThoughtNode(summary="b")
        c = ThoughtNode(summary="c")
        graph.add_node(a)
        graph.add_node(b)
        graph.add_node(c)
        graph.add_edge(a.id, b.id)
        graph.add_edge(b.id, c.id)
        graph.add_edge(c.id, a.id)
        loops = graph.find_loops()
        assert len(loops) >= 1

    def test_GetMostInfluential_ShouldReturnTopNodes(self):
        graph = ThoughtGraph()
        center = ThoughtNode(summary="center")
        graph.add_node(center)
        for _ in range(5):
            leaf = ThoughtNode()
            graph.add_node(leaf)
            graph.add_edge(center.id, leaf.id)
        top = graph.get_most_influential(3)
        assert len(top) == 1
        assert top[0][0].id == center.id
        assert top[0][1] == 5.0

    def test_GetSubgraph_Depth1_ShouldIncludeNeighbors(self):
        graph = ThoughtGraph()
        center = ThoughtNode(summary="center")
        a = ThoughtNode(summary="a")
        b = ThoughtNode(summary="b")
        graph.add_node(center)
        graph.add_node(a)
        graph.add_node(b)
        graph.add_edge(center.id, a.id)
        graph.add_edge(center.id, b.id)
        sub = graph.get_subgraph(center.id, depth=1)
        assert sub.node_count == 3

    def test_GetSubgraph_MissingNode_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        sub = graph.get_subgraph(uuid4())
        assert sub.node_count == 0

    def test_GetThoughtClusters_Disconnected_ShouldReturnMultiple(self):
        graph = ThoughtGraph()
        a1 = ThoughtNode()
        a2 = ThoughtNode()
        b1 = ThoughtNode()
        graph.add_node(a1)
        graph.add_node(a2)
        graph.add_node(b1)
        graph.add_edge(a1.id, a2.id)
        clusters = graph.get_thought_clusters()
        assert len(clusters) == 2
        sizes = sorted(len(c) for c in clusters)
        assert sizes == [1, 2]

    def test_GetThoughtClusters_SingleNode_ShouldReturnOne(self):
        graph = ThoughtGraph()
        graph.add_node(ThoughtNode())
        clusters = graph.get_thought_clusters()
        assert len(clusters) == 1

    def test_LRUEviction_WhenExceedMax_ShouldEvictOldest(self):
        graph = ThoughtGraph(max_nodes=3)
        n1 = ThoughtNode(summary="first")
        n2 = ThoughtNode(summary="second")
        n3 = ThoughtNode(summary="third")
        n4 = ThoughtNode(summary="fourth")
        graph.add_node(n1)
        graph.add_node(n2)
        graph.add_node(n3)
        graph.add_node(n4)
        assert graph.node_count == 3
        assert graph.get_node(n1.id) is None
        assert graph.get_node(n4.id) is not None

    def test_LRUEviction_TouchedNodes_ShouldBePreserved(self):
        graph = ThoughtGraph(max_nodes=3)
        n1 = ThoughtNode()
        n2 = ThoughtNode()
        n3 = ThoughtNode()
        n4 = ThoughtNode()
        graph.add_node(n1)
        graph.add_node(n2)
        graph.add_node(n3)
        graph.get_node(n1.id)
        graph.add_node(n4)
        assert graph.get_node(n1.id) is not None
        assert graph.get_node(n2.id) is None

    def test_ToDict_RoundTrip_ShouldPreserveData(self):
        graph = ThoughtGraph()
        a = ThoughtNode(summary="node_a")
        b = ThoughtNode(summary="node_b")
        graph.add_node(a)
        graph.add_node(b)
        graph.add_edge(a.id, b.id, ThoughtRelation.SUPPORTS, 0.9)
        data = graph.to_dict()
        restored = ThoughtGraph.from_dict(data)
        assert restored.node_count == 2
        assert restored.edge_count == 1

    def test_ComputeContentHash_DifferentInputs_ShouldDiffer(self):
        h1 = ThoughtGraph.compute_content_hash("hello")
        h2 = ThoughtGraph.compute_content_hash("world")
        assert h1 != h2

    def test_ComputeContentHash_SameInput_ShouldMatch(self):
        h1 = ThoughtGraph.compute_content_hash("test")
        h2 = ThoughtGraph.compute_content_hash("test")
        assert h1 == h2

    def test_GetMostInfluential_EmptyGraph_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        assert graph.get_most_influential() == []

    def test_GetThoughtClusters_EmptyGraph_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        assert graph.get_thought_clusters() == []

    def test_FindLoops_EmptyGraph_ShouldBeEmpty(self):
        graph = ThoughtGraph()
        assert graph.find_loops() == []

    def test_NodeCount_EmptyGraph_ShouldBeZero(self):
        graph = ThoughtGraph()
        assert graph.node_count == 0
        assert graph.edge_count == 0


class TestIntegrationWithCycle:
    @pytest.mark.asyncio
    async def test_CycleRunner_ShouldCreateThoughtNodes(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        graph = ThoughtGraph()
        runner = CognitiveCycleRunner(thought_graph=graph)
        await runner.run("test")
        assert graph.node_count > 0

    @pytest.mark.asyncio
    async def test_CycleRunner_NodesHaveSequencesEdges(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        graph = ThoughtGraph()
        runner = CognitiveCycleRunner(thought_graph=graph)
        await runner.run("hello")
        assert graph.edge_count > 0

    @pytest.mark.asyncio
    async def test_CycleRunner_ContradictionDetection(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        graph = ThoughtGraph()
        runner = CognitiveCycleRunner(thought_graph=graph)
        await runner.run("test")
        assert graph.find_contradictions() is not None

    @pytest.mark.asyncio
    async def test_CycleRunner_MultipleCycles_ShouldAccumulate(self):
        from krnlai.core.cycle import CognitiveCycleRunner

        graph = ThoughtGraph()
        runner = CognitiveCycleRunner(thought_graph=graph)
        await runner.run("first")
        count1 = graph.node_count
        await runner.run("second")
        assert graph.node_count > count1
