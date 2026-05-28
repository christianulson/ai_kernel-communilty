# Contributing

## Development Principles

1. **TDD (Test-Driven Development)** — Write tests before production code
2. **Safety First** — All contributions must respect the safety model
3. **Deterministic Tests** — Tests must be offline, fast, and deterministic
4. **Local-First** — No external service dependencies in community mode

## Test Requirements

- Framework: **xUnit** (C#) / **pytest** (Python)
- All tests must run offline
- No hardcoded API keys or secrets
- Each individual test < 5 seconds
- Same input → same result

## Build and Test

### .NET Projects

```bash
# Restore
dotnet restore krnlai.slnx

# Build
dotnet build krnlai.slnx

# Test all
dotnet test krnlai.slnx

# Test specific project
dotnet test tests/KrnlAI.Tests/KrnlAI.Tests.csproj

# Test with filter
dotnet test --filter "FullyQualifiedName~SafetyCommandTests"
```

### Python SDK

```bash
cd Community/sdk/python
pip install -e .
pytest
```

### Web/Tauri

- `Community/src/KrnlAI.Desktop.Tauri` contains the cross-platform desktop surface
- Desktop work now includes auth state, API keys, privacy controls, and P2P/WebRTC signaling docs

```bash
# See `Community/src/KrnlAI.Desktop.Tauri/` for the React frontend
```

## Pull Request Checklist

- [ ] Build with no errors or warnings
- [ ] All tests passing
- [ ] New code has corresponding tests
- [ ] Tests written BEFORE code (TDD)
- [ ] No tests depending on network/external services
- [ ] Updated documentation if applicable
- [ ] Changes are focused on a single concern

## Good First Issues

- Documentation improvements
- Sample code and patterns
- Small CLI workflow additions
- Test coverage for existing features
- Provider examples for new LLM backends
- Bug fixes with simple reproduction

## Code Style

- C#: Nullable enabled, implicit usings, latest language version
- Python: Type hints, async/await patterns
- Follow existing conventions in the codebase you're modifying

## Getting Help

- **GitHub Issues** — Bug reports and feature requests
- **GitHub Discussions** — Q&A and community showcase
