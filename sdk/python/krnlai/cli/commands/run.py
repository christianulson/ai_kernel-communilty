from __future__ import annotations

from rich.console import Console
from rich.panel import Panel
from rich.prompt import Prompt

from krnlai.cli.main import _create_agent, _run_debug_cycle

console = Console()


async def cmd_run(args: list) -> None:
    interactive = "--interactive" in args or "-i" in args
    text = " ".join(a for a in args if not a.startswith("-"))

    agent = _create_agent()
    if interactive:
        await _run_interactive(agent)
    elif text:
        await _run_once(agent, text)
    else:
        console.print("[yellow]Usage: krnlai run [--interactive/-i] [text][/yellow]")


async def _run_once(agent, text: str) -> None:
    with console.status("[bold green]Processing...[/bold green]"):
        result = await agent.run(text)
    if result.error:
        console.print(f"[red]Error:[/red] {result.error}")
    else:
        console.print(Panel(result.output, title="Response"))


async def _run_interactive(agent) -> None:
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
