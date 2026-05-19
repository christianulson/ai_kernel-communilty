# Arquitetura

Krnl-AI Community e organizado em torno de uma separacao estrita entre estado deterministico do kernel e traducao voltada ao LLM.

## Principios de Design

1. **Separacao de Poderes** — O kernel possui estado, validacao e politicas. O LLM traduz e propoe, nunca escreve estado diretamente.
2. **Seguranca por Design** — Toda acao passa por multiplas camadas de seguranca antes da execucao.
3. **Local-First** — Todo o estado e armazenado localmente via SQLite. Nenhuma infraestrutura hospedada necessaria.
4. **Nucleo Deterministico** — O kernel e totalmente deterministico dadas as mesmas entradas.

## Arquitetura de Alto Nivel

```
┌─────────────────────────────────────────────┐
│              CLI / Desktop / Editores         │
│  (Interfaces de usuario e ferramentas dev)   │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│              Sidecar (API HTTP)               │
│  Execucao Agente → Verificacoes Seg. → Local │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│           Kernel Embutido (In-Process)        │
│  ┌──────────┐ ┌────────┐ ┌──────────────┐   │
│  │ Memoria  │ │Camadas │ │Mecanismo     │   │
│  │ Sistema  │ │Segur.  │ │Politicas     │   │
│  └──────────┘ └────────┘ └──────────────┘   │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│        Armazenamento Local (SQLite)           │
│  Episodios │ Semantica │ Politicas │ Config  │
└─────────────────────────────────────────────┘
```

## Visao Geral dos Componentes

| Componente | Responsabilidade |
|-----------|----------------|
| **Kernel Embutido** | Gerenciamento de estado, memoria, seguranca, politicas, aprendizado |
| **Sidecar** | API HTTP com pipeline de seguranca e proxy empresarial opcional |
| **CLI** | Interface de terminal com TUI para sessoes interativas |
| **SDK (Python/.NET)** | Acesso programatico ao runtime cognitivo |
| **Apps Desktop** | Aplicativos desktop nativos WPF e Tauri |
| **Extensoes de Editor** | Integracoes VS Code e Visual Studio IDE |

## Fluxo de Dados

```
Entrada Usuario → Verif. Seguranca → Recuperacao Memoria → Avaliacao
→ Planejamento → Governanca → Execucao → Resultado → Aprendizado
```

## Pipeline de Seguranca

Toda execucao de agente passa por verificacoes de seguranca em camadas:

1. **Guarda Adversarial** — Detecta injecao de prompt e tentativas de jailbreak
2. **Regras Fundamentais (R01-R20)** — Aplica 20 regras inquebravels
3. **Aplicador Etico** — Valida contra principios eticos
4. **Limitacao de Taxa** — Previne abuso e exaustao de recursos

Para documentacao detalhada de seguranca, veja [Sistema de Seguranca](../06-Safety/safety-system.md).

## Pilha de Tecnologia

| Componente | Tecnologia |
|-----------|------------|
| Runtime | .NET 10 / Python 3.10+ |
| Armazenamento | SQLite (local), MySQL (proxy empresarial) |
| Vetores | Armazenamento vetorial SQLite (local), Qdrant (proxy empresarial) |
| Cache | Em memoria (local), Redis (proxy empresarial) |
| Desktop | WPF (.NET), Tauri (Rust + React) |
| SDK | .NET (netstandard2.0), Python (3.10+) |
