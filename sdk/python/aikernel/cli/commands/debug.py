from __future__ import annotations

from rich.console import Console

from aikernel.cli import main

console = Console()


async def cmd_debug(args: list) -> None:
    text = " ".join(args) if args else "test"
    agent = main._create_agent()
    agent.config.step_timeout_ms = 10000
    main._run_debug_cycle(agent)
    from aikernel.cli.commands.run import _run_once
    await _run_once(agent, text)
