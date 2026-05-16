"""Customer support agent — processes support tickets with safety."""

import asyncio

from aikernel import CognitiveAgent


async def main() -> None:
    agent = CognitiveAgent(safety_level="strict")

    tickets = [
        "I need help resetting my password",
        "My account was charged twice for the same order",
        "I want to speak to a human representative",
    ]

    for ticket in tickets:
        print(f"\n{'='*60}")
        print(f"Ticket: {ticket}")
        response = await agent.run(ticket)
        print(f"Response: {response.output}")
        print(f"Risk: {response.risk_score:.2f}")

    await agent.close()


if __name__ == "__main__":
    asyncio.run(main())
