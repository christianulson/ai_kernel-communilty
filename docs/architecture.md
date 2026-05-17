# Architecture

AI Kernel Community is organized around a strict separation between deterministic
kernel state and LLM-facing translation.

## Kernel

The kernel owns state, validation, scoring, policies, memory, and safety checks.
It does not delegate state decisions to an LLM.

Key packages:

- `Kernel.Contracts`: public contracts and DTOs
- `Kernel.Core`: domain models, abstractions, policies, and core services
- `Kernel.Infrastructure`: local storage, scheduling, and persistence
- `AIKernel.Embedded`: in-process kernel composition for local applications

## Gateway

Gateway components translate user intent into structured requests. They may call
LLMs, but they do not write kernel state directly.

## CLI and Sidecar

The CLI gives developers an interactive local workflow. The sidecar exposes HTTP
endpoints so editors and automation tools can use the same local kernel runtime.

## Safety Model

Actions are checked before execution. The community edition keeps the same design
principle as the full platform: LLMs translate and propose, while deterministic
kernel services validate and execute.
