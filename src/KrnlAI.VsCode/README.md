# Krnl-AI for VS Code

[![CI](https://github.com/krnl-ai/kernel/actions/workflows/ci.yml/badge.svg)](https://github.com/krnl-ai/kernel/actions/workflows/ci.yml)
![Visual Studio Code Version](https://img.shields.io/badge/VS%20Code-^1.118.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

**Krnl-AI** is a cognitive coding agent for VS Code. Unlike traditional AI assistants, Krnl-AI combines a full cognitive cycle (10 steps), multi-layer safety system (20 immutable rules), and 7 types of memory to provide intelligent, safe, and context-aware code assistance.

---

## Features

### 🧠 Cognitive Chat
Ask anything with full context of your editor — open file, selection, workspace diagnostics.

### ✨ Inline Code Completion
AI-powered autocomplete as you type, with LRU-cached responses for speed.

### 🛡️ Safety System (R01-R20)
Every action is validated against 20 immutable safety rules before execution.

### 💾 7 Types of Memory
Semantic, Episodic, Procedural, Working, Emotional, Autobiographical, Temporal — the agent learns and remembers across sessions.

### 🎯 Slash Commands (21 commands)
| Command | Description |
|---------|-------------|
| `/explain` | Explain selected code |
| `/fix` | Fix diagnostics or selection |
| `/test` | Generate unit test |
| `/refactor` | Refactor selected code |
| `/review` | Review code changes |
| `/doc` | Generate documentation |
| `/commit` | Create git commit |
| `/run` | Run terminal command |
| `/build` | Build the project |
| `/task` | Multi-step agentic task |
| And 11 more... |

### 📊 Cognitive Dashboard
Real-time view of: cognitive cycle stages, risk levels, active policies, emotional state (VAD), memory metrics, and system health.

---

## Quick Start

### 1. Install
Install from [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=krnlai.krnlai-vscode).

### 2. Start the backend
Choose one:
- **Docker** (recommended): `docker compose up -d` in the project root
- **Sidecar** (embedded): Set `krnlai.mode: embedded`
- **Cloud**: Set `krnlai.endpoint` to your remote API

### 3. Start coding
- Open any code file
- Select some code → right-click → "Krnl-AI: Explain"
- Or press `Ctrl+Shift+E` to explain
- Or open the chat panel from the activity bar

### 4. Enable advanced features (opt-in)
Set these in VS Code settings:
| Setting | Description |
|---------|-------------|
| `krnlai.codingAgent.enabled` | Enable coding commands |
| `krnlai.codingAgent.inlineCompletion` | Inline autocomplete |
| `krnlai.codingAgent.terminal` | Terminal command execution |
| `krnlai.codingAgent.git` | Git command integration |
| `krnlai.codingAgent.agenticLoops` | Multi-step agentic loops |

---

## Requirements

- **VS Code** ^1.118.0
- **Backend**: Docker Compose (MySQL + Redis + Qdrant + KrnlAI API + LLM Gateway) OR Sidecar OR remote API

Full Docker setup:
```bash
git clone https://github.com/krnl-ai/kernel.git
cd kernel
docker compose up -d
```

---

## Extension Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `krnlai.endpoint` | `http://localhost:5235` | Backend API endpoint |
| `krnlai.mode` | `localApi` | Runtime mode: embedded, localApi, remoteApi |
| `krnlai.codingAgent.enabled` | `false` | Enable coding agent features |

All settings are prefixed under `krnlai.*`.

---

## Architecture

```
VS Code Extension ←→ KrnlAI Sidecar / API
                        ↓
                KrnlAI Cognitive Engine
                  ┌─────────────┐
                  │ 10-step     │
                  │ Cognitive   │
                  │ Cycle       │
                  ├─────────────┤
                  │ Safety R01  │
                  │ - R20       │
                  ├─────────────┤
                  │ 7 Memory    │
                  │ Types       │
                  ├─────────────┤
                  │ Emotional   │
                  │ State (VAD) │
                  └─────────────┘
```

---

## Why Krnl-AI vs Other AI Assistants?

| Feature | Krnl-AI | Codex | Copilot |
|---------|:-------:|:-----:|:-------:|
| Cognitive Cycle (10 steps) | ✅ | ❌ | ❌ |
| Safety System (R01-R20) | ✅ | ❌ | ❌ |
| 7 Memory Types | ✅ | ❌ | ❌ |
| Emotional State | ✅ | ❌ | ❌ |
| Meta-Cognition | ✅ | ❌ | ❌ |
| Policy Engine (A/B experiments) | ✅ | ❌ | ❌ |
| Audit Trail (immutable) | ✅ | ❌ | ❌ |
| 13+ LLM Providers | ✅ | ❌ | ❌ |
| Multi-platform | 7 platforms | 1 | 2 |

---

## Telemetry

This extension does NOT collect telemetry. All data stays on your infrastructure.

---

## License

MIT
