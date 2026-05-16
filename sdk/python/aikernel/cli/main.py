from __future__ import annotations

import asyncio
import sys

from rich.console import Console
from rich.panel import Panel
from rich.prompt import Prompt
from rich.table import Table

from aikernel.core.cycle import CognitiveCycleRunner, CycleConfig

console = Console()


async def _run_once(agent: CognitiveCycleRunner, text: str) -> None:
    with console.status("[bold green]Processing...[bold green]"):
        result = await agent.run(text)
    if result.error:
        console.print(f"[red]Error:[/red] {result.error}")
    else:
        console.print(Panel(result.output, title="Response"))


async def _run_interactive(agent: CognitiveCycleRunner) -> None:
    console.print("[bold cyan]AI Kernel Interactive Mode[/bold cyan]")
    console.print("Type 'exit' to quit, 'help' for commands\n")
    while True:
        text = Prompt.ask("[bold green]>[/bold green]")
        if text.lower() in ("exit", "quit"):
            break
        if text.lower() == "help":
            console.print("Commands: exit, help, debug, status")
            continue
        if text.lower() == "debug":
            _run_debug_cycle(agent)
            continue
        if text.lower() == "status":
            console.print(f"Safety level: {agent.config.safety_level}")
            console.print(f"Max iterations: {agent.config.max_iterations}")
            console.print(f"Emotions: {'enabled' if agent.config.enable_emotions else 'disabled'}")
            continue
        await _run_once(agent, text)


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


async def _cmd_init(args: list) -> None:
    name = args[0] if args else "my-agent"
    import os
    os.makedirs(name, exist_ok=True)
    with open(f"{name}/agent.py", "w") as f:
        f.write('from aikernel import CognitiveAgent\n\nagent = CognitiveAgent(safety_level="strict")\n')
    with open(f"{name}/.env", "w") as f:
        f.write("# Add your API keys here\n# OPENAI_API_KEY=sk-...\n")
    console.print(f"[green]Created agent project: {name}/[/green]")


async def _cmd_run(args: list) -> None:
    interactive = "--interactive" in args or "-i" in args
    text = " ".join(a for a in args if not a.startswith("-"))

    agent = _create_agent()
    if interactive:
        await _run_interactive(agent)
    elif text:
        await _run_once(agent, text)
    else:
        console.print("[yellow]Usage: aikernel run [--interactive/-i] [text][/yellow]")


async def _cmd_debug(args: list) -> None:
    text = " ".join(args) if args else "test"
    agent = _create_agent()
    agent.config.step_timeout_ms = 10000
    _run_debug_cycle(agent)
    await _run_once(agent, text)


def app() -> None:
    args = sys.argv[1:]
    if not args:
        console.print("[bold]AI Kernel CLI[/bold]")
        console.print("Usage:")
        console.print("  aikernel init <name>         Create a new agent project")
        console.print("  aikernel run [text]          Run agent once")
        console.print("  aikernel run --interactive   Interactive mode")
        console.print("  aikernel debug [text]        Debug cognitive cycle")
        return

    cmd = args[0]
    cmd_args = args[1:]

    try:
        if cmd == "init":
            asyncio.run(_cmd_init(cmd_args))
        elif cmd == "run":
            asyncio.run(_cmd_run(cmd_args))
        elif cmd == "debug":
            asyncio.run(_cmd_debug(cmd_args))
        else:
            console.print(f"[red]Unknown command: {cmd}[/red]")
    except KeyboardInterrupt:
        console.print("\n[yellow]Interrupted[/yellow]")
    except Exception as e:
        console.print(f"[red]Error:[/red] {e}")
        sys.exit(1)
