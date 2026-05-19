from __future__ import annotations

import asyncio
import sys

from rich.console import Console
from rich.table import Table

from krnlai.core.cycle import CognitiveCycleRunner, CycleConfig

console = Console()


def _create_agent(
    safety_level: str = "strict",
    max_iterations: int = 10,
    enable_emotions: bool = True,
) -> CognitiveCycleRunner:
    config = CycleConfig(
        safety_level=safety_level,
        max_iterations=max_iterations,
        enable_emotions=enable_emotions,
    )
    return CognitiveCycleRunner(config=config)


def _run_debug_cycle(agent: CognitiveCycleRunner) -> None:
    table = Table(title="Cognitive Cycle Debug")
    table.add_column("Step")
    table.add_column("Status")
    table.add_column("Duration")
    table.add_column("Data")

    events = []

    def on_event(event):
        events.append(event)
        step_name = event.step.value if hasattr(event.step, "value") else str(event.step)
        table.add_row(
            step_name,
            event.status,
            f"{event.duration_ms:.1f}ms",
            str(event.data)[:50],
        )

    agent.on_event(on_event)
    console.print(table)


def app() -> None:
    args = sys.argv[1:]
    if not args:
        console.print("[bold]Krnl-AI CLI[/bold]")
        console.print("Usage:")
        console.print("  krnlai init <name>         Create a new agent project")
        console.print("  krnlai run [text]          Run agent once")
        console.print("  krnlai run --interactive   Interactive mode")
        console.print("  krnlai debug [text]        Debug cognitive cycle")
        console.print("  krnlai security <command>  Security audit, benchmark, report")
        console.print("  krnlai deploy <target>     Generate Docker/K8s files")
        console.print("  krnlai thought <sub>       Thought Graph commands")
        console.print("  krnlai reasoning <sub>     Reasoning quality commands")
        return

    cmd = args[0]
    cmd_args = args[1:]

    try:
        if cmd == "init":
            from krnlai.cli.commands.init import init_project
            name = cmd_args[0] if cmd_args else "my-agent"
            init_project(name)
        elif cmd == "run":
            from krnlai.cli.commands.run import cmd_run
            asyncio.run(cmd_run(cmd_args))
        elif cmd == "debug":
            from krnlai.cli.commands.debug import cmd_debug
            asyncio.run(cmd_debug(cmd_args))
        elif cmd == "security":
            from krnlai.cli.commands.security import cmd_security
            asyncio.run(cmd_security(cmd_args))
        elif cmd == "deploy":
            from krnlai.cli.commands.deploy import cmd_deploy
            asyncio.run(cmd_deploy(cmd_args))
        elif cmd == "thought":
            from krnlai.cli.commands.thoughtgraph import cmd_thoughtgraph
            cmd_thoughtgraph(cmd_args)
        elif cmd == "reasoning" or cmd == "cognitive":
            from krnlai.cli.commands.reasoning import cmd_reasoning
            cmd_reasoning(cmd_args)
        else:
            console.print(f"[red]Unknown command: {cmd}[/red]")
    except KeyboardInterrupt:
        console.print("\n[yellow]Interrupted[/yellow]")
    except Exception as e:
        console.print(f"[red]Error:[/red] {e}")
        sys.exit(1)
