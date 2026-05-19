from __future__ import annotations

from typing import List

from rich.console import Console
from rich.table import Table

from krnlai.core.cognition.thought_graph import ThoughtGraph

console = Console()


def _load_graph() -> ThoughtGraph:
    return ThoughtGraph()


def cmd_thoughtgraph(args: List[str]) -> None:
    if not args:
        _show_help()
        return

    sub = args[0]
    sub_args = args[1:]

    if sub == "graph":
        _cmd_graph(sub_args)
    elif sub == "chain":
        _cmd_chain(sub_args)
    elif sub == "contradictions":
        _cmd_contradictions()
    elif sub == "loops":
        _cmd_loops()
    elif sub == "influential":
        _cmd_influential(sub_args)
    elif sub == "clusters":
        _cmd_clusters()
    else:
        console.print(f"[red]Unknown thought subcommand: {sub}[/red]")
        _show_help()


def _show_help() -> None:
    console.print("[bold]Thought Graph Commands[/bold]")
    console.print("  krnlai thought graph --id <id>        Show subgraph for a thought node")
    console.print("  krnlai thought chain --id <id>        Reasoning chain for a thought node")
    console.print("  krnlai thought contradictions         List contradictions")
    console.print("  krnlai thought loops                  Detect reasoning loops")
    console.print("  krnlai thought influential --limit N  Most influential thoughts")
    console.print("  krnlai thought clusters               Thought clusters")


def _cmd_graph(args: List[str]) -> None:
    graph = _load_graph()
    node_id = _parse_id(args)
    if not node_id:
        _show_table_summary(graph)
        return
    from uuid import UUID
    sub = graph.get_subgraph(UUID(node_id))
    console.print(f"[green]Subgraph centered on {node_id}: {sub.node_count} nodes, {sub.edge_count} edges[/green]")
    _show_graph_table(sub)


def _cmd_chain(args: List[str]) -> None:
    graph = _load_graph()
    node_id = _parse_id(args)
    if not node_id:
        console.print("[red]Usage: krnlai thought chain --id <node-id>[/red]")
        return
    from uuid import UUID
    chain = graph.get_reasoning_chain(UUID(node_id))
    table = Table(title=f"Reasoning Chain for {node_id}")
    table.add_column("#")
    table.add_column("Step")
    table.add_column("Category")
    table.add_column("Summary")
    for i, node in enumerate(reversed(chain)):
        table.add_row(str(i + 1), node.step.value, node.category.value, node.summary[:40])
    console.print(table)


def _cmd_contradictions() -> None:
    graph = _load_graph()
    pairs = graph.find_contradictions()
    if not pairs:
        console.print("[yellow]No contradictions found[/yellow]")
        return
    table = Table(title=f"Contradictions ({len(pairs)})")
    table.add_column("Source")
    table.add_column("Target")
    for src, tgt, edge in pairs:
        table.add_row(src.summary[:30], tgt.summary[:30])
    console.print(table)


def _cmd_loops() -> None:
    graph = _load_graph()
    loops = graph.find_loops()
    if not loops:
        console.print("[green]No loops detected[/green]")
        return
    console.print(f"[yellow]Detected {len(loops)} loop(s)[/yellow]")
    for i, loop in enumerate(loops[:5]):
        summaries = " → ".join(n.summary[:20] for n in loop)
        console.print(f"  Loop {i + 1}: {summaries}")


def _cmd_influential(args: List[str]) -> None:
    graph = _load_graph()
    limit = 10
    if args and args[0] == "--limit" and len(args) > 1:
        try:
            limit = int(args[1])
        except ValueError:
            pass
    top = graph.get_most_influential(limit)
    table = Table(title=f"Most Influential Thoughts (top {limit})")
    table.add_column("Score")
    table.add_column("Step")
    table.add_column("Category")
    table.add_column("Summary")
    for node, score in top:
        table.add_row(f"{score:.1f}", node.step.value, node.category.value, node.summary[:40])
    console.print(table)


def _cmd_clusters() -> None:
    graph = _load_graph()
    clusters = graph.get_thought_clusters()
    if not clusters:
        console.print("[yellow]No clusters found[/yellow]")
        return
    console.print(f"[green]Found {len(clusters)} cluster(s)[/green]")
    for i, cluster in enumerate(clusters[:10]):
        console.print(f"  Cluster {i + 1}: {len(cluster)} nodes")
        for node in cluster[:3]:
            console.print(f"    - {node.step.value}: {node.summary[:40]}")


def _parse_id(args: List[str]) -> str | None:
    if args and args[0] == "--id" and len(args) > 1:
        return args[1]
    return None


def _show_table_summary(graph: ThoughtGraph) -> None:
    table = Table(title="Thought Graph Summary")
    table.add_column("Metric")
    table.add_column("Value")
    table.add_row("Nodes", str(graph.node_count))
    table.add_row("Edges", str(graph.edge_count))
    if graph.node_count > 0:
        top = graph.get_most_influential(3)
        for i, (node, score) in enumerate(top):
            table.add_row(f"Top {i + 1}", f"{node.summary[:30]} (score: {score:.1f})")
    console.print(table)


def _show_graph_table(graph: ThoughtGraph) -> None:
    if graph.node_count == 0:
        return
    table = Table(title="Graph Nodes")
    table.add_column("ID")
    table.add_column("Step")
    table.add_column("Category")
    table.add_column("Summary")
    for nid, node in list(graph._nodes.items())[:20]:
        table.add_row(str(nid)[:8], node.step.value, node.category.value, node.summary[:30])
    console.print(table)
