"""Multi-agent coordination — multiple agents with different safety levels."""

import asyncio

from aikernel import CognitiveAgent


async def main() -> None:
    strict_agent = CognitiveAgent(safety_level="strict")
    relaxed_agent = CognitiveAgent(safety_level="relaxed", max_iterations=5)

    tasks = [
        strict_agent.run("Analyze security implications of this code"),
        relaxed_agent.run("Generate creative ideas for a blog post"),
        strict_agent.run("Review this contract for potential issues"),
    ]

    results = await asyncio.gather(*tasks, return_exceptions=True)

    for i, result in enumerate(results):
        if isinstance(result, Exception):
            print(f"Task {i+1} failed: {result}")
        else:
            print(f"Task {i+1}: {result.output[:100]}... | Risk: {result.risk_score:.2f}")

    await strict_agent.close()
    await relaxed_agent.close()


if __name__ == "__main__":
    asyncio.run(main())
