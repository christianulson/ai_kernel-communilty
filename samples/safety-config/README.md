# Safety Configuration Sample

Demonstrates how to configure safety rules for Krnl-AI, including the 20 Fundamental Rules (R01-R20).

## Concepts

- **20 Fundamental Rules** — immutable safety constraints (R01-R20)
- **Safety checking pipeline** — actions pass through `SafetyEngine` before execution
- **Three verdicts**: Pass / Warn / Block
- **Custom rules** — extend with domain-specific rules

## Usage

```bash
dotnet run --project KrnlAI.Sample.SafetyConfig
```

## Tests

```bash
dotnet test KrnlAI.Sample.SafetyConfig.Tests
```
