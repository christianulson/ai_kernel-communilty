# AI Kernel Community

O motor cognitivo do AI Kernel para uso local — memória persistente, busca vetorial,
skills que evoluem, e segurança em camadas. Tudo rodando na sua máquina, sem servidor.

```bash
dotnet tool install -g AIKernel.Cli
aikernel chat --local
```

## O que você pode fazer

- **Agente local com LLM de sua escolha** — OpenAI, Anthropic, Ollama (grátis) ou qualquer provedor compatível
- **Memória persistente em SQLite** — conversas, estado, preferências sobrevivem a restart
- **Busca semântica local** — vector store embutido (BLOB + cosine similarity em RAM)
- **Skills que evoluem sozinhos** — o LLM cria, refina e melhora skills baseado no uso
- **Segurança em camadas** — 20 regras fundamentais (R01-R20), safety checker, meta-critic
- **Zero dependências** — sem Docker, sem MySQL, sem Redis

## Componentes

| Projeto | Descrição | Licença |
|---|---|---|
| `Kernel.Contracts` | DTOs e interfaces públicas | MIT |
| `AIKernel.Cli` | CLI interativa (`aikernel chat --local`) | MIT |
| `AIKernel.Sidecar` | Servidor HTTP local para integração com VSCode | MIT |
| `AIKernel.VsCode` | Extensão para Visual Studio Code | MIT |

## Quick start

```bash
# 1. Instalar
dotnet tool install -g AIKernel.Cli

# 2. Configurar token do seu LLM
aikernel config set provider openai
aikernel config set api_key sk-...

# 3. Usar
aikernel chat --local
```

## Documentação

- [Guia de início rápido](doc/GETTING_STARTED.md)
- [Análises e planos](doc/analises/)
- [Roadmap](doc/backlog/todo/)

## Licença

MIT — veja [LICENSE](LICENSE).
