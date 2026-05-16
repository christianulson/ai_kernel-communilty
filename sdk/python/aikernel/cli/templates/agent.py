from aikernel import CognitiveAgent

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
