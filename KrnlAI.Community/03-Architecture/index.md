# Architecture

Krnl-AI Community is organized around a strict separation between deterministic kernel state and LLM-facing translation.

## Design Principles

1. **Separation of Powers** — The kernel owns state, validation, and policies. The LLM translates and proposes, never writes state directly.
2. **Safety by Design** — Every action passes through multiple safety layers before execution.
3. **Local-First** — All state is stored locally via SQLite. No hosted infrastructure required.
4. **Deterministic Core** — The kernel is fully deterministic given the same inputs.

## High-Level Architecture

```
┌─────────────────────────────────────────────┐
│              CLI / Desktop / Editors          │
│  (User interfaces and developer tools)       │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│              Sidecar (HTTP API)               │
│  Agent Run → Safety Checks → Local/Proxy     │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│           Embedded Kernel (In-Process)        │
│  ┌──────────┐ ┌────────┐ ┌──────────────┐   │
│  │ Memory   │ │Safety  │ │Policy Engine │   │
│  │ System   │ │Layers  │ │& Learning    │   │
│  └──────────┘ └────────┘ └──────────────┘   │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│           Local Storage (SQLite)              │
│  Episodes │ Semantic │ Policies │ Settings   │
└─────────────────────────────────────────────┘
```

## Component Overview

| Component | Responsibility |
|-----------|----------------|
| **Embedded Kernel** | State management, memory, safety, policies, learning |
| **Sidecar** | HTTP API with safety pipeline and optional enterprise proxy |
| **CLI** | Terminal interface with TUI for interactive sessions |
| **SDK (Python/.NET)** | Programmatic access to the cognitive runtime |
| **Desktop Apps** | WPF and Tauri native desktop applications |
| **Editor Extensions** | VS Code and Visual Studio IDE integrations |

## Data Flow

```
User Input → Safety Check → Memory Recall → Evaluation
→ Planning → Governance → Execution → Outcome → Learning
```

## Safety Pipeline

Every agent run flows through layered safety checks:

1. **Adversarial Guard** — Detects prompt injection and jailbreak attempts
2. **Fundamental Rules (R01-R20)** — Enforces 20 unbreakable rules
3. **Ethical Enforcer** — Validates against ethical principles
4. **Rate Limiting** — Prevents abuse and resource exhaustion

For detailed safety documentation, see [[06-Safety/index|Safety System]].

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 / Python 3.10+ |
| Storage | SQLite (local), MySQL (enterprise proxy) |
| Vectors | SQLite vector store (local), Qdrant (enterprise proxy) |
| Cache | In-memory (local), Redis (enterprise proxy) |
| Desktop | WPF (.NET), Tauri (Rust + React) |
| SDK | .NET (netstandard2.0), Python (3.10+) |
