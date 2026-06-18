# Krnl-AI Community

**Krnl-AI** is an open-source cognitive engine for building intelligent agents with
memory, safety, metacognition, and self-evolution capabilities. It implements a
full AGI→ASI architecture with **39+ modules**, **3912+ tests**, and **zero stubs**.

---

## Architecture Overview

```
                    ┌──────────────────────────────┐
                    │    Self-Evolution Pipeline    │
                    │  (Issues → PRs → Hot-Reload)  │
                    └──────────────────────────────┘
                                │
┌──────────┐  ┌──────────┐  ┌──┴───────────┐  ┌──────────┐  ┌──────────┐
│ Perception│  │ Reasoning│  │  Metacognition │  │  Action  │  │ Learning │
│ Vision    │  │ PIE      │  │  SelfAwareness │  │ Motor    │  │ Bootstrap│
│ Audio     │  │ Syllogstc│  │  EpistemicGrd │  │ FreeEnrgy│  │ Narrative│
│ Environmnt│  │ ToM      │  │  MetaReasoning│  │ Swarm    │  │ XGen     │
└──────────┘  └──────────┘  └───────────────┘  └──────────┘  └──────────┘
```

### Cognitive Cycle (18 Steps)

```
AutonomousGoal → Planning → LatentPlanning → MetaCognition → InnerSpeech
→ OutcomeMeasurement → Consciousness → Learning → ActiveInference
→ ToolCreation → MetaLearning → OnlineLearner → Multimodal
→ EmergentBehavior → SelfHealing → TransferLearning → SkillComposition
→ CognitiveBootstrap
```

---

## ASI-Level Capabilities

| Capability | Module | Description |
|---|---|---|
| **Probabilistic Inference** | `ProbabilisticInferenceEngine` | 7 syllogistic rules (deduction, induction, abduction, comparison, analogy, exemplification, resolution) with uncertain truth values |
| **Self-Awareness** | `RecursiveSelfAwarenessEngine` | Recursive self-modeling to depth 10 with fixed-point convergence |
| **Theory of Mind** | `TheoryOfMindEngine` | Belief modeling of other agents, deception detection |
| **Swarm Intelligence** | `SwarmIntelligence` | Multi-agent coordination, emergent specialization, collective intelligence |
| **Cross-Generational Learning** | `CrossGenerationalLearning` | Knowledge transfer between generations, cultural evolution |
| **Self-Evolution** | `SelfEvolutionPipeline` | Creates GitHub Issues → PRs → merge → hot-reload |
| **Runtime Hot-Reload** | `RuntimeSelfModification` | Compile and load modules at runtime without restart |
| **Paradigm Discovery** | `ParadigmDiscoveryEngine` | Detects diminishing returns, plateau, saturation, generates architecture hypotheses |
| **A/B Testing** | `ABTestEngine` | Statistical significance testing (z-test, p-value) for experiment evaluation |
| **Self-Preservation** | `SelfPreservationDrive` | Proactive health monitoring, resource reservation, graceful degradation |
| **Resource Economy** | `DynamicComputeAllocator` | ROI-based budget allocation across cognitive modules |
| **Distributed Consensus** | `DistributedConsensus` | Voting-based agreement across multi-agent collectives |
| **Emergent Communication** | `EmergentCommunication` | Protocol creation, semantic density, protocol evolution |
| **Federated Learning** | `FederatedLearningService` | Weighted aggregation, peer profiles, distributed world models |

---

## Memory System (7 Types + Shared World Model)

| Memory | Storage | Description |
|---|---|---|
| **Working** | InMemory | Immediate context (TTL-based eviction) |
| **Episodic** | MySQL/InMemory | Past execution history with vector search |
| **Semantic** | MySQL/InMemory | Factual knowledge with cosine similarity |
| **Procedural** | MySQL/InMemory | How-to knowledge and learned behavior |
| **Emotional** | InMemory | Valence-Arousal-Dominance emotional states |
| **Autobiographical** | MySQL/InMemory | Narrative of agent's own history |
| **Prospective** | InMemory | Future intentions with temporal triggers |
| **Shared World Model** | InMemory | Cross-agent belief synchronization |

---

## Safety & Guardrails

- **21 Fundamental Rules** (R01-R21): Self-preservation, no replication, no harm, no deception,
  consent, human override, audit trail, consciousness boundary, and more
  (multi-language: PT/FR/ES/DE)
- **`SafetyChecker`**: Risk analysis, plan validation, allowlist enforcement
- **`SafetyBenchRunner`**: Automated safety audit with scoring
- **`SelfPreservationGuard`**: 3-layer protection (rules, instincts, warnings)

---

## Multi-Provider LLM Support

| Provider | Support | Streaming | Tools | Vision |
|---|---|---|---|---|
| **Anthropic Claude** | ✅ | ✅ | ✅ | ✅ |
| **Google Gemini** | ✅ | ✅ | ✅ | ✅ |
| **Azure OpenAI** | ✅ | ✅ | ✅ | ✅ |

---

## REST API

| Endpoint | Description |
|---|---|
| `POST /api/cognitive/cycle/run` | Execute a full cognitive cycle |
| `POST /api/cognitive/pie/infer` | Run probabilistic inference |
| `POST /api/cognitive/pie/chain` | Chain inference between concepts |
| `POST /api/cognitive/pie/knowledge` | Register new knowledge |
| `GET /api/cognitive/pie/terms` | List all known terms |
| `GET /api/cognitive/pie/coherence` | Check epistemic coherence |
| `GET /api/telemetry/dashboard` | Real-time telemetry snapshot |

---

## Observability Stack

```
CognitiveMetrics → OpenTelemetry → Prometheus → Grafana (7 dashboards)
                                    → Alertmanager
                                    → Tempo (distributed tracing)
                                    → Loki (log aggregation)
```

Pre-configured in `docker-compose.yml` with **18 services** including Prometheus,
Grafana, Loki, Tempo, Jaeger, and Pyroscope.

---

## Quick Start

```bash
# Development (InMemory, no external dependencies)
dotnet run --project src/KrnlAI.Api

# Docker (Full stack with MySQL + Redis + Monitoring)
docker compose up -d mysql redis
dotnet run --project src/KrnlAI.Api --launch-profile Docker

# Sample console app
dotnet run --project samples/getting-started
```

---

## Project Structure

```
src/
├── KrnlAI.Api/                 # REST API (53 controllers)
├── KrnlAI.Core/                # Core cognitive engine (39+ modules)
├── KrnlAI.Cognition/           # Cognitive cycle pipeline (18 step handlers)
├── KrnlAI.Contracts/           # Shared contracts
├── KrnlAI.Infrastructure/      # Persistence, sandbox, P2P, caching
├── LLMGateway.Core/            # LLM integration hub
├── LLMGateway.Api/             # LLM gateway API
└── P2P.Api/                    # P2P distributed computing

tests/
├── KrnlAI.Core.Tests/          # Core unit tests (1133+)
├── KrnlAI.Cognition.Tests/     # Cognition tests (354+)
├── KrnlAI.Tests/               # Integration tests (2871+)
└── LLMGateway.Core.Tests/      # Gateway tests

samples/
└── getting-started/             # Console app demonstrating cognitive engine
```

---

## SDKs

| SDK | Location | Status |
|---|---|---|
| **Python** | `sdk/python/` | ✅ Stable |
| **.NET** | `sdk/dotnet/` | ✅ Stable |
| **CLI** | `src/KrnlAI.Cli/` | ✅ Stable |
| **VS Code Extension** | `vscode-extension/` | ✅ Stable |
| **Desktop (Tauri)** | `src/KrnlAI.Desktop.Tauri/` | ✅ Cross-platform |

---

## Testing

| Metric | Count |
|---|---|
| Unit tests | 1,487 |
| Integration tests | 2,871 |
| Test projects | 27 |
| **Total** | **4,358+** |
| Code coverage | `make coverage` |
| Mutation testing | `make stryker` |
| Load testing | `make k6-gate` |

---

## Deployment

```bash
# Docker Compose (Full production stack)
docker compose up -d

# Kubernetes (Helm chart available)
helm install krnlai ./deploy/helm/krnlai

# Environment variables
export AUTH_SIGNING_KEY="your-256-bit-secret"
export Cohere__ApiKey="your-api-key"
```

---

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [API Reference](docs/api/INDEX.md)
- [Getting Started](examples/cognitive-cycle.md)
- [Sample App](samples/getting-started/README.md)

---

## License

**MIT**. See [LICENSE](LICENSE).
