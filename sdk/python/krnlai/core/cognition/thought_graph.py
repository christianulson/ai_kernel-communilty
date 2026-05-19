from __future__ import annotations

from collections import deque
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from hashlib import sha256
from typing import Any, Dict, List, Optional, Tuple
from uuid import UUID, uuid4

from krnlai.core.models.cognitive import CycleStep
from krnlai.core.models.moment import MomentCategory
from krnlai.core.models.thought import ThoughtCategory, ThoughtClassification


class ThoughtRelation(str, Enum):
    CAUSES = "causes"
    SUPPORTS = "supports"
    CONTRADICTS = "contradicts"
    ELABORATES = "elaborates"
    GENERALIZES = "generalizes"
    SPECIALIZES = "specializes"
    SEQUENCES = "sequences"
    PARALLEL = "parallel"
    ANALOGOUS_TO = "analogous_to"
    ABSTRACTION_OF = "abstraction_of"
    INSTANCE_OF = "instance_of"
    REPLIES_TO = "replies_to"


@dataclass
class ThoughtNode:
    id: UUID = field(default_factory=uuid4)
    cycle_id: UUID = field(default_factory=uuid4)
    step: CycleStep = CycleStep.SENSOR
    category: ThoughtCategory = ThoughtCategory.CONTEXT_AWARE
    moment_category: Optional[MomentCategory] = None
    content_hash: str = ""
    classification: Optional[ThoughtClassification] = None
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    summary: str = ""


@dataclass
class ThoughtEdge:
    source_id: UUID = field(default_factory=uuid4)
    target_id: UUID = field(default_factory=uuid4)
    relation: ThoughtRelation = ThoughtRelation.SEQUENCES
    weight: float = 1.0
    discovered_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))


class ThoughtGraph:
    def __init__(self, max_nodes: int = 10000) -> None:
        self._nodes: Dict[UUID, ThoughtNode] = {}
        self._edges: List[ThoughtEdge] = []
        self._adjacency: Dict[UUID, List[Tuple[UUID, ThoughtRelation, float]]] = {}
        self._reverse_adj: Dict[UUID, List[Tuple[UUID, ThoughtRelation, float]]] = {}
        self._max_nodes = max_nodes
        self._access_order: List[UUID] = []

    @property
    def node_count(self) -> int:
        return len(self._nodes)

    @property
    def edge_count(self) -> int:
        return len(self._edges)

    def add_node(self, node: ThoughtNode) -> None:
        if node.id in self._nodes:
            self._touch(node.id)
            return
        if len(self._nodes) >= self._max_nodes:
            self._evict_lru()
        self._nodes[node.id] = node
        self._adjacency.setdefault(node.id, [])
        self._reverse_adj.setdefault(node.id, [])
        self._access_order.append(node.id)

    def add_edge(
        self,
        source_id: UUID,
        target_id: UUID,
        relation: ThoughtRelation = ThoughtRelation.SEQUENCES,
        weight: float = 1.0,
    ) -> None:
        if source_id not in self._nodes or target_id not in self._nodes:
            return
        self._edges.append(ThoughtEdge(
            source_id=source_id,
            target_id=target_id,
            relation=relation,
            weight=weight,
        ))
        self._adjacency[source_id].append((target_id, relation, weight))
        self._reverse_adj[target_id].append((source_id, relation, weight))

    def get_node(self, node_id: UUID) -> Optional[ThoughtNode]:
        node = self._nodes.get(node_id)
        if node:
            self._touch(node_id)
        return node

    def get_reasoning_chain(self, node_id: UUID) -> List[ThoughtNode]:
        chain: List[ThoughtNode] = []
        visited: set[UUID] = set()
        queue: deque[UUID] = deque()
        if node_id in self._nodes:
            queue.append(node_id)
            visited.add(node_id)
        while queue:
            current = queue.popleft()
            node = self._nodes.get(current)
            if node:
                chain.append(node)
            for source_id, _, _ in self._reverse_adj.get(current, []):
                if source_id not in visited:
                    visited.add(source_id)
                    queue.append(source_id)
        return chain

    def find_contradictions(self) -> List[Tuple[ThoughtNode, ThoughtNode, ThoughtEdge]]:
        results: List[Tuple[ThoughtNode, ThoughtNode, ThoughtEdge]] = []
        for edge in self._edges:
            if edge.relation == ThoughtRelation.CONTRADICTS:
                src = self._nodes.get(edge.source_id)
                tgt = self._nodes.get(edge.target_id)
                if src and tgt:
                    results.append((src, tgt, edge))
        return results

    def find_loops(self) -> List[List[ThoughtNode]]:
        visited: set[UUID] = set()
        rec_stack: set[UUID] = set()
        loops: List[List[ThoughtNode]] = []
        path: List[UUID] = []

        def dfs(node_id: UUID) -> None:
            visited.add(node_id)
            rec_stack.add(node_id)
            path.append(node_id)
            for neighbor, _, _ in self._adjacency.get(node_id, []):
                if neighbor not in visited:
                    dfs(neighbor)
                elif neighbor in rec_stack:
                    cycle_start = path.index(neighbor)
                    cycle_ids = path[cycle_start:] + [neighbor]
                    cycle_nodes = [self._nodes[nid] for nid in cycle_ids if nid in self._nodes]
                    if cycle_nodes:
                        loops.append(cycle_nodes)
            path.pop()
            rec_stack.discard(node_id)

        for nid in list(self._nodes.keys()):
            if nid not in visited:
                dfs(nid)
        return loops

    def get_most_influential(self, limit: int = 10) -> List[Tuple[ThoughtNode, float]]:
        scores: List[Tuple[UUID, float]] = []
        for nid, neighbors in self._adjacency.items():
            out_degree = len(neighbors)
            if out_degree > 0:
                scores.append((nid, float(out_degree)))
        scores.sort(key=lambda x: -x[1])
        result: List[Tuple[ThoughtNode, float]] = []
        for nid, score in scores[:limit]:
            node = self._nodes.get(nid)
            if node:
                result.append((node, score))
        return result

    def get_subgraph(self, node_id: UUID, depth: int = 2) -> "ThoughtGraph":
        sub = ThoughtGraph(max_nodes=self._max_nodes)
        visited: set[UUID] = set()
        queue: deque[Tuple[UUID, int]] = deque()
        if node_id in self._nodes:
            queue.append((node_id, 0))
            visited.add(node_id)
        while queue:
            current, d = queue.popleft()
            node = self._nodes.get(current)
            if node:
                sub.add_node(node)
            if d >= depth:
                continue
            for neighbor, rel, weight in self._adjacency.get(current, []):
                if neighbor not in visited:
                    visited.add(neighbor)
                    queue.append((neighbor, d + 1))
                sub.add_edge(current, neighbor, rel, weight)
            for source, rel, weight in self._reverse_adj.get(current, []):
                if source not in visited:
                    visited.add(source)
                    queue.append((source, d + 1))
                sub.add_edge(source, current, rel, weight)
        return sub

    def get_thought_clusters(self) -> List[List[ThoughtNode]]:
        visited: set[UUID] = set()
        clusters: List[List[ThoughtNode]] = []

        def bfs(start: UUID) -> List[ThoughtNode]:
            component: List[ThoughtNode] = []
            q: deque[UUID] = deque([start])
            visited.add(start)
            while q:
                current = q.popleft()
                node = self._nodes.get(current)
                if node:
                    component.append(node)
                for neighbor, _, _ in self._adjacency.get(current, []):
                    if neighbor not in visited:
                        visited.add(neighbor)
                        q.append(neighbor)
                for source, _, _ in self._reverse_adj.get(current, []):
                    if source not in visited:
                        visited.add(source)
                        q.append(source)
            return component

        for nid in list(self._nodes.keys()):
            if nid not in visited:
                cluster = bfs(nid)
                if cluster:
                    clusters.append(cluster)
        return clusters

    def to_dict(self) -> Dict[str, Any]:
        return {
            "nodes": [
                {
                    "id": str(n.id),
                    "cycle_id": str(n.cycle_id),
                    "step": n.step.value if hasattr(n.step, "value") else str(n.step),
                    "category": n.category.value if hasattr(n.category, "value") else str(n.category),
                    "moment_category": n.moment_category.value
                    if n.moment_category and hasattr(n.moment_category, "value")
                    else str(n.moment_category) if n.moment_category else None,
                    "content_hash": n.content_hash,
                    "summary": n.summary,
                    "created_at": n.created_at.isoformat(),
                }
                for n in self._nodes.values()
            ],
            "edges": [
                {
                    "source_id": str(e.source_id),
                    "target_id": str(e.target_id),
                    "relation": e.relation.value,
                    "weight": e.weight,
                }
                for e in self._edges
            ],
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "ThoughtGraph":
        g = cls()
        for nd in data.get("nodes", []):
            node = ThoughtNode(
                id=UUID(nd["id"]),
                cycle_id=UUID(nd["cycle_id"]),
                step=CycleStep(nd["step"]),
                category=ThoughtCategory(nd["category"]),
                moment_category=MomentCategory(nd["moment_category"]) if nd.get("moment_category") else None,
                content_hash=nd.get("content_hash", ""),
                summary=nd.get("summary", ""),
                created_at=(
                    datetime.fromisoformat(nd["created_at"])
                    if "created_at" in nd
                    else datetime.now(timezone.utc)
                ),
            )
            g._nodes[node.id] = node
        for ed in data.get("edges", []):
            edge = ThoughtEdge(
                source_id=UUID(ed["source_id"]),
                target_id=UUID(ed["target_id"]),
                relation=ThoughtRelation(ed["relation"]),
                weight=ed.get("weight", 1.0),
            )
            g._edges.append(edge)
            g._adjacency.setdefault(edge.source_id, []).append((edge.target_id, edge.relation, edge.weight))
            g._reverse_adj.setdefault(edge.target_id, []).append((edge.source_id, edge.relation, edge.weight))
        return g

    @staticmethod
    def compute_content_hash(payload: str) -> str:
        return sha256(payload.encode("utf-8")).hexdigest()[:16]

    def _touch(self, node_id: UUID) -> None:
        if node_id in self._access_order:
            self._access_order.remove(node_id)
            self._access_order.append(node_id)

    def _evict_lru(self) -> None:
        if not self._access_order:
            return
        oldest = self._access_order.pop(0)
        self._nodes.pop(oldest, None)
        self._adjacency.pop(oldest, None)
        self._reverse_adj.pop(oldest, None)
        self._edges[:] = [e for e in self._edges if e.source_id != oldest and e.target_id != oldest]
