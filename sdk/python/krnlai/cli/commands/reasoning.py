from __future__ import annotations

from typing import List

from rich.console import Console
from rich.table import Table

from krnlai.core.cognition.adaptive import AdaptiveProcessor

console = Console()


def _get_processor() -> AdaptiveProcessor:
    return AdaptiveProcessor()


def cmd_reasoning(args: List[str]) -> None:
    if not args:
        _show_help()
        return

    sub = args[0]

    if sub == "status":
        _cmd_status()
    elif sub == "issues":
        _cmd_issues()
    elif sub == "mode":
        _cmd_mode()
    elif sub == "history":
        _cmd_history()
    elif sub == "cognitive":
        _cmd_cognitive(args[1:])
    else:
        console.print(f"[red]Unknown reasoning subcommand: {sub}[/red]")
        _show_help()


def _show_help() -> None:
    console.print("[bold]Reasoning Commands[/bold]")
    console.print("  krnlai reasoning status         Show current reasoning quality")
    console.print("  krnlai reasoning issues         List reasoning issues")
    console.print("  krnlai reasoning mode           Show current processing mode")
    console.print("  krnlai reasoning history        Show reasoning quality history")
    console.print("  krnlai cognitive mode           Show adaptive processing mode")


def _cmd_status() -> None:
    proc = _get_processor()
    avg = proc.get_average_quality()
    table = Table(title="Reasoning Status")
    table.add_column("Metric")
    table.add_column("Value")
    table.add_row("Average Quality", f"{avg:.2f}")
    table.add_row("Current Mode", proc.current_mode.mode)
    table.add_row("History Entries", str(len(proc.history)))
    console.print(table)


def _cmd_issues() -> None:
    proc = _get_processor()
    all_issues: List[str] = []
    for entry in proc.history:
        all_issues.extend(entry.issues)
    if not all_issues:
        console.print("[green]No issues recorded[/green]")
        return
    from collections import Counter
    counts = Counter(all_issues)
    table = Table(title="Reasoning Issues")
    table.add_column("Issue")
    table.add_column("Count")
    for issue, count in counts.most_common():
        table.add_row(issue, str(count))
    console.print(table)


def _cmd_mode() -> None:
    proc = _get_processor()
    mode = proc.current_mode
    table = Table(title="Current Processing Mode")
    table.add_column("Parameter")
    table.add_column("Value")
    table.add_row("Mode", mode.mode)
    table.add_row("Max Iterations", str(mode.max_iterations))
    table.add_row("Safety Level", mode.safety_level)
    table.add_row("Planning Depth", str(mode.planning_depth))
    table.add_row("Emotional Sensitivity", f"{mode.emotional_sensitivity:.1f}")
    table.add_row("Novelty Seeking", f"{mode.novelty_seeking:.1f}")
    console.print(table)


def _cmd_history() -> None:
    proc = _get_processor()
    entries = proc.get_history(20)
    if not entries:
        console.print("[yellow]No history recorded[/yellow]")
        return
    table = Table(title="Reasoning History (last 20)")
    table.add_column("Iteration")
    table.add_column("Quality")
    table.add_column("Coherence")
    table.add_column("Completeness")
    table.add_column("Soundness")
    table.add_column("Mode")
    for e in entries:
        table.add_row(
            str(e.iteration),
            f"{e.quality:.2f}",
            f"{e.coherence:.2f}",
            f"{e.completeness:.2f}",
            f"{e.soundness:.2f}",
            e.mode,
        )
    console.print(table)


def _cmd_cognitive(args: List[str]) -> None:
    proc = _get_processor()
    mode = proc.current_mode
    console.print(f"[bold]Adaptive Mode:[/bold] {mode.mode}")
    console.print(f"  Max iterations: {mode.max_iterations}")
    console.print(f"  Safety level: {mode.safety_level}")
    console.print(f"  Planning depth: {mode.planning_depth}")
    console.print(f"  Emotional sensitivity: {mode.emotional_sensitivity}")
    console.print(f"  Novelty seeking: {mode.novelty_seeking}")
