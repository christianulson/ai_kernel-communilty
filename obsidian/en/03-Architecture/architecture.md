# Architecture

Krnl-AI Community is organized around a strict separation between deterministic kernel state and LLM-facing translation.

## Design Principles

1. **Separation of Powers** — The kernel owns state, validation, and policies. The LLM translates and proposes, never writes state directly.
2. **Safety by Design** — Every action passes through multiple safety layers before execution.
3. **Local-First** — All state is stored locally via SQLite. No hosted infrastructure required.
4. **Deterministic Core** — The kernel is fully deterministic given the same inputs.

## High-Level Architecture

```
┌──────────────────────────────────────────────┐
│              CLI / Desktop / Editors          │
│  (User interfaces and developer tools)       │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│              Sidecar (HTTP API)               │
│  Agent Run → Safety Checks → Local/Proxy     │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│           Embedded Kernel (In-Process)        │
│  ┌──────────┐ ┌──────────┐ ┌─────────────┐  │
│  │ Memory   │ │Cognitive │ │Policy Engine│  │
│  │ System   │ │Cycle     │ │& Learning   │  │
│  └──────────┘ └──────────┘ └─────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌─────────────┐  │
│  │Safety    │ │Emotion   │ │Metacognition│  │
│  │Layers    │ │Model     │ │             │  │
│  └──────────┘ └──────────┘ └─────────────┘  │
└──────────────────────┬───────────────────────┘
                       │
┌──────────────────────▼───────────────────────┐
│           Local Storage (SQLite)              │
│  Episodes │ Semantic │ Policies │ Emotions   │
│  Procedural │ Autobiographical │ Settings    │
└──────────────────────────────────────────────┘
```

## Cognitive Modules

The kernel is composed of specialized cognitive modules:

| Module | Responsibility |
|--------|----------------|
| **Memory System** | Episodic, semantic, procedural, working, emotional, autobiographical, and prospective memory types |
| **Cognitive Cycle** | 10-step perception-to-learning processing pipeline |
| **Safety Layers** | Multi-layered guard against malicious input and unsafe actions |
| **Emotion Model** | VAD (Valence-Arousal-Dominance) dimensional model influencing risk perception |
| **Metacognition** | Self-observation of emotional state, risk level, and cognitive biases |
| **Policy Engine** | Learned decision policies updated from outcomes |
| **Attention System** | Feature extraction, prioritization, and focus allocation |

## Component Overview

| Component | Responsibility |
|-----------|----------------|
| **Embedded Kernel** | State management, memory, cognitive cycle, safety, policies, learning, emotions |
| **Sidecar** | HTTP API with safety pipeline, optional enterprise proxy, and P2P signaling |
| **CLI** | Terminal interface with TUI for interactive sessions |
| **SDK (Python/.NET)** | Programmatic access to the cognitive runtime |
| **Desktop Apps** | WPF and Tauri native desktop applications with auth, privacy, and P2P/WebRTC surfaces |
| **Editor Extensions** | VS Code and Visual Studio IDE integrations |

## Data Flow

```
User Input → Safety Check → Memory Recall → Evaluation
→ Metacognition → Planning → Governance → Execution
→ Outcome Recording → Learning → Emotional Update
```

## Safety Pipeline

Every agent run flows through layered safety checks:

1. **Adversarial Guard** — Detects prompt injection and jailbreak attempts
2. **Fundamental Rules (R01-R20)** — Enforces 20 unbreakable rules
3. **Ethical Enforcer** — Validates against ethical principles
4. **Input Validation** — Schema validation on all inputs
5. **Allowlist** — Only registered actions are permitted
6. **Rate Limiting** — Prevents abuse and resource exhaustion

For detailed safety documentation, see [Safety System](../06-Safety/safety-system.md).

## Desktop P2P / WebRTC

The desktop surfaces now include local peer-to-peer video calling support.

- `VideoCallViewModel` manages call state and peer selection in WPF
- `WebRtcService` opens a WebSocket signaling session at `/signaling/webrtc`
- `SettingsViewModel` exposes STUN/TURN configuration
- Tauri settings persist auth state and expose the desktop UI surfaces that complement the WPF call flow

## Technology Stack

| Component | Community (Local) | Enterprise (Proxy) |
|-----------|-------------------|---------------------|
| Runtime | .NET 10 / Python 3.10+ | .NET 10 / Python 3.10+ |
| Storage | SQLite | MySQL |
| Vectors | SQLite vector store | Qdrant HNSW |
| Cache | In-memory | Redis |
| Safety | Full pipeline | Full pipeline + MetaCritic |
| Desktop | WPF (.NET), Tauri (Rust + React) | WPF (.NET), Tauri (Rust + React) |
| SDK | .NET (netstandard2.0), Python (3.10+) | .NET (netstandard2.0), Python (3.10+) |
