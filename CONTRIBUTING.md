# Contributing to Krnl-AI Community

Thanks for helping improve Krnl-AI Community. This repository follows the same
engineering rules as the private Krnl-AI codebase: tests first, deterministic
behavior, and no hidden external-service dependency in tests.

## Development Setup

1. Install the latest .NET SDK supported by the repository.
2. Clone the repository and restore dependencies.
3. Run the test suite before changing code.
4. Write a failing test for the behavior you want to add or fix.
5. Implement the smallest change that makes the test pass.

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## TDD Rules

- Write tests before production code.
- Keep tests offline, fast, and deterministic.
- Add tests for each new service, model, public abstraction, and user-visible CLI
  workflow.
- Do not replace a failing test by removing behavior. Fix the implementation.

## Pull Requests

Before opening a pull request:

- Run `dotnet build --no-restore`.
- Run `dotnet test --no-build`.
- Update documentation when behavior or public APIs change.
- Keep changes focused on one feature or bug fix.

## Good First Issues

Good starter contributions include documentation fixes, sample improvements,
small CLI workflows, tests for existing behavior, and provider-specific examples.
