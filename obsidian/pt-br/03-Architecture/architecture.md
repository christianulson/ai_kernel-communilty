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

## Modulos Cognitivos

| Modulo | Responsabilidade |
|--------|----------------|
| **Sistema de Memoria** | Memoria episodica, semantica, procedural, operacional, emocional, autobiografica e prospectiva |
| **Ciclo Cognitivo** | Pipeline de processamento de 10 etapas (percepcao a aprendizado) |
| **Camadas de Seguranca** | Protecao multicamadas contra entradas maliciosas e acoes inseguras |
| **Modelo Emocional** | Modelo dimensional VAD (Valencia-Excitação-Dominancia) |
| **Metacognicao** | Auto-observacao do estado emocional, nivel de risco e vieses cognitivos |
| **Mecanismo de Politicas** | Politicas de decisao aprendidas a partir de resultados |
| **Sistema de Atencao** | Extracao de caracteristicas, priorizacao e alocacao de foco |
| **Modelos de Mundo** | Modelos preditivos do ambiente (baseados em JEPA) para simulacao e planejamento |
| **Raciocinio Causal** | Inferencia de causa-efeito baseada em grafos |
| **Inferencia Ativa** | Selecao de acoes baseada no principio da energia livre |
| **Consolidacao de Sonhos** | Geracao de cenarios offline e consolidacao de memoria |
| **Aprendizado Continuo** | Pipeline completo: memoria → analise causal → modelo de mundo → sonho → consolidacao |

## Visao Geral dos Componentes

| Componente | Responsabilidade |
|-----------|----------------|
| **Kernel Embutido** | Gerenciamento de estado, memoria, seguranca, politicas, aprendizado, modelos de mundo |
| **Sidecar** | API HTTP com pipeline de seguranca, proxy empresarial opcional e sinalizacao P2P |
| **CLI** | Interface de terminal com TUI para sessoes interativas |
| **SDK (Python/.NET)** | Acesso programatico ao runtime cognitivo |
| **Apps Desktop** | Aplicativos desktop nativos WPF e Tauri com auth, privacidade e superficies P2P/WebRTC |
| **Extensoes de Editor** | Integracoes VS Code e Visual Studio IDE |

## Fluxo de Dados

```
Entrada Usuario → Verif. Seguranca → Recuperacao Memoria → Avaliacao
→ Metacognicao → Planejamento → Governanca → Execucao
→ Registro Resultado → Aprendizado → Atualizacao Emocional
```

## Pipeline de Seguranca

Toda execucao de agente passa por verificacoes de seguranca em camadas:

1. **Guarda Adversarial** — Detecta injecao de prompt e tentativas de jailbreak
2. **Regras Fundamentais (R01-R20)** — Aplica 20 regras inquebravels
3. **Aplicador Etico** — Valida contra principios eticos
4. **Validacao de Entrada** — Validacao de schema em todas as entradas
5. **Lista de Permissao** — Apenas acoes registradas sao permitidas
6. **Limitacao de Taxa** — Previne abuso e exaustao de recursos

Para documentacao detalhada de seguranca, veja [Sistema de Seguranca](../06-Safety/safety-system.md).

Para documentacao detalhada de seguranca, veja [Sistema de Seguranca](../06-Safety/safety-system.md).

## P2P / WebRTC no Desktop

As superficies desktop agora incluem suporte local a chamadas de video peer-to-peer.

- `VideoCallViewModel` gerencia estado da chamada e selecao de peer no WPF
- `WebRtcService` abre uma sessao WebSocket de sinalizacao em `/signaling/webrtc`
- `SettingsViewModel` expõe configuracao STUN/TURN
- A superficie Tauri persiste o estado de auth e complementa o fluxo visual de chamadas do WPF

## Pilha de Tecnologia

| Componente | Tecnologia |
|-----------|------------|
| Runtime | .NET 10 / Python 3.10+ |
| Armazenamento | SQLite (local), MySQL (proxy empresarial) |
| Vetores | Armazenamento vetorial SQLite (local), Qdrant (proxy empresarial) |
| Cache | Em memoria (local), Redis (proxy empresarial) |
| Desktop | WPF (.NET), Tauri (Rust + React) |
| SDK | .NET (netstandard2.0), Python (3.10+) |
