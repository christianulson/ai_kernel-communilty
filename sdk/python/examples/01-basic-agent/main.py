"""Basic agent example — run a cognitive cycle with safety checks."""

import asyncio

from aikernel import CognitiveAgent


async def main() -> None:
    agent = CognitiveAgent(safety_level="strict")

    response = await agent.run("analyze the impact of AI on education")
    print(f"Output: {response.output}")
    print(f"Risk score: {response.risk_score:.2f}")
    print(f"Duration: {response.duration_ms:.0f}ms")

    if response.emotional_delta:
        print(f"Emotional state: {response.emotional_delta}")

    await agent.close()


if __name__ == "__main__":
    asyncio.run(main())
