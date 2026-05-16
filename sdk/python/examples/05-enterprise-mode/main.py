"""Enterprise mode — connects to C# AI Kernel backend."""

import asyncio

from aikernel import CognitiveAgent


async def main() -> None:
    agent = CognitiveAgent(
        mode="enterprise",
        endpoint="http://localhost:5001",
        api_key="sk-enterprise-key",
    )

    print(f"Mode: {agent.mode}")
    print(f"Enterprise: {agent.is_enterprise}")

    response = await agent.run("Process this with enterprise backend")
    print(f"Response: {response.output}")
    print(f"Duration: {response.duration_ms:.0f}ms")

    await agent.close()


if __name__ == "__main__":
    asyncio.run(main())
