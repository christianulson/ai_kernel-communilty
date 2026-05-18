from __future__ import annotations

import os

from rich.console import Console

console = Console()

TEMPLATE_AGENT_PY = '''from krnlai import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")


async def main() -> None:
    response = await agent.run("analyze this dataset")
    print(f"Response: {response.output}")
    print(f"Risk score: {response.risk_score}")
    if response.safety_verdict:
        print(f"Safety: {response.safety_verdict.allowed}")


if __name__ == "__main__":
    import asyncio
    asyncio.run(main())
'''

TEMPLATE_ENV = '''# Krnl-AI Configuration
# Uncomment and set your LLM provider API key:

# OPENAI_API_KEY=sk-...
# ANTHROPIC_API_KEY=sk-ant-...
# OLLAMA_BASE_URL=http://localhost:11434
'''

TEMPLATE_REQUIREMENTS = '''krnlai>=0.1.0
python-dotenv>=1.0.0
'''


def init_project(name: str) -> None:
    os.makedirs(name, exist_ok=True)
    with open(f"{name}/agent.py", "w") as f:
        f.write(TEMPLATE_AGENT_PY)
    with open(f"{name}/.env", "w") as f:
        f.write(TEMPLATE_ENV)
    with open(f"{name}/requirements.txt", "w") as f:
        f.write(TEMPLATE_REQUIREMENTS)
    console.print(f"[green]Created agent project: {name}/[/green]")
    console.print(f"  {name}/agent.py")
    console.print(f"  {name}/.env")
    console.print(f"  {name}/requirements.txt")
    console.print("\nNext steps:")
    console.print(f"  cd {name}")
    console.print("  pip install -r requirements.txt")
    console.print("  krnlai run --interactive")
