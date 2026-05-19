# Kernel vs Gateway

The architecture follows a **separation of powers** principle: the kernel makes decisions and manages state, while the LLM translates natural language into structured actions.

## The Golden Rule

> **The LLM never decides or writes state directly. It only translates.**

## Kernel Responsibilities

| Aspect | Description |
|--------|-------------|
| **State** | Manages all persistent and working state |
| **Memory** | Episodic, semantic, working, and emotional memory |
| **Safety** | Evaluates actions against 20 fundamental rules |
| **Policies** | Stores and enforces learned policies |
| **Evaluation** | Assesses signals and computes risk scores |
| **Learning** | Updates policies and semantic memory from outcomes |

## Gateway Responsibilities (when proxied to enterprise)

| Aspect | Description |
|--------|-------------|
| **Translation** | Converts natural language to structured commands |
| **Planning** | Breaks objectives into execution plans |
| **Narration** | Converts results back to human-readable text |
| **Coordination** | Orchestrates the plan-safety-execute flow |

## Community vs Enterprise

| Feature | Community (Local) | Enterprise (Proxied) |
|---------|------------------|---------------------|
| Storage | SQLite | MySQL + Qdrant |
| Vectors | SQLite vector store | Qdrant HNSW |
| Cache | In-memory | Redis |
| Safety | Full pipeline | Full pipeline + MetaCritic |
| Auth | None (localhost) | JWT + RBAC |
| Scale | Single user | Multi-tenant |
