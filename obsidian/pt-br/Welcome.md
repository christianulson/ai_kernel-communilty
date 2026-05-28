# Comunidade Krnl-AI

> Um runtime cognitivo para construir agentes locais com memória persistente, verificações de segurança, habilidades em evolução, ferramentas de desenvolvimento e superficies desktop P2P.

Krnl-AI Community é a edição aberta e local-first do Krnl-AI. Ela roda inteiramente em sua máquina sem exigir infraestrutura hospedada. Todo o estado é armazenado localmente via SQLite, e você pode trazer seu próprio provedor LLM (OpenAI, Ollama, Anthropic, etc.).

## Início Rápido

```bash
dotnet tool install -g KrnlAI.Cli
krnlai config set provider ollama
krnlai chat --local
```

## O Que Você Pode Construir

- Agentes locais com seu LLM preferido
- Memória persistente com SQLite e busca semântica
- Habilidades que podem ser criadas, refinadas, exportadas e instaladas
- Execução consciente de segurança com 20 regras fundamentais (R01-R20)
- Integrações com editores VS Code e Visual Studio
- Aplicativos desktop multiplataforma via WPF e Tauri
- Sessões de video peer-to-peer via sinalização WebRTC

## Pacotes

| Pacote | Propósito |
|---------|-----------|
| `KrnlAI.Cli` | CLI local e TUI interativa |
| `KrnlAI.Sidecar` | Sidecar HTTP local para ferramentas e editores |
| `KrnlAI.Contracts` | DTOs e interfaces públicas |
| `KrnlAI.Embedded` | Kernel in-process para aplicativos comunitários |

## SDKs

| SDK | Linguagem | Origem |
|-----|-----------|--------|
| Python SDK | Python 3.10+ | `sdk/python/` |
| .NET SDK | netstandard2.0 | `sdk/dotnet/` |

## Mapa da Documentação

| Seção | Descrição |
|---------|-------------|
| [Primeiros Passos](01-Getting-Started/getting-started.md) | Instalar, configurar, primeira execução |
| [Referência da CLI](02-CLI/cli-reference.md) | Todos os comandos e opções da CLI |
| [Arquitetura](03-Architecture/architecture.md) | Design do sistema e princípios |
| [Ciclo Cognitivo](04-Cognitive-Cycle/cognitive-cycle.md) | Pipeline de processamento cognitivo de 10 etapas |
| [Sistema de Memória](05-Memory/memory-system.md) | Memória episódica, semântica, operacional e emocional |
| [Sistema de Segurança](06-Safety/safety-system.md) | 20 regras fundamentais, guarda adversarial, ética |
| [Sistema Emocional](07-Emotions/emotion-system.md) | Modelo VAD, dor/recompensa, memória emocional |
| [Guia do SDK](08-SDK/sdk-guide.md) | Documentação dos SDKs Python e .NET |
| [API Sidecar](09-API/sidecar-api.md) | Referência dos endpoints da API HTTP e sinalização P2P |
| [Aplicativos Desktop](10-Desktop/desktop-apps.md) | Aplicativos desktop WPF e Tauri, API keys, privacidade e P2P |
| [Extensões de Editor](11-Editors/editor-extensions.md) | Extensões para VS Code e Visual Studio |
| [Exemplos](12-Samples/samples.md) | Projetos de exemplo e padrões |
| [Integrações](13-Integrations/integrations.md) | LangChain, CrewAI, AutoGen, FastAPI |
| [Contribuindo](14-Contributing/contributing.md) | Como contribuir com o projeto |
| [Matriz Comparativa](comparative-matrix.md) | Krnl-AI vs 15 alternativas de mercado — comparação recurso por recurso |

## Comunidade

- **GitHub Issues** — Relatórios de bug e solicitações de funcionalidades
- **GitHub Discussions** — Perguntas, ideias e demonstrações (quando habilitado)
- **Licença** — MIT
