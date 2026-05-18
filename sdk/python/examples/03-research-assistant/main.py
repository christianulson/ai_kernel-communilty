"""Research assistant — analyzes topics with memory across sessions."""

import asyncio

from krnlai import CognitiveAgent


async def main() -> None:
    agent = CognitiveAgent(safety_level="strict", enable_emotions=True)

    queries = [
        "What are the latest advances in transformer architectures?",
        "How do these compare to state space models?",
        "Summarize the key findings from the first two queries",
    ]

    for query in queries:
        print(f"\nQuery: {query}")
        response = await agent.run(query)
        print(f"Analysis: {response.output[:200]}...")

    print(f"\nEpisodic memory entries: {len(agent._standalone.episodic_memory._episodes)}")
    print(f"Semantic memory facts: {len(agent._standalone.semantic_memory._facts)}")

    await agent.close()


if __name__ == "__main__":
    asyncio.run(main())
