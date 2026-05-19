# Primeiros Passos

## Pre-requisitos

- .NET 10 SDK
- Uma chave de provedor LLM ou um endpoint local compativel com OpenAI (Ollama, etc.)

## Instalar a CLI

```bash
dotnet tool install -g KrnlAI.Cli
```

Para verificar a instalacao:

```bash
krnlai --version
```

## Configurar um Provedor

### OpenAI

```bash
krnlai config set provider openai
krnlai config set api_key sk-...
```

### Ollama (Local)

```bash
krnlai config set provider ollama
krnlai config set endpoint http://localhost:11434/v1
krnlai config set model llama3.1
```

### Anthropic

```bash
krnlai config set provider anthropic
krnlai config set api_key sk-ant-...
```

### OpenRouter

```bash
krnlai config set provider openrouter
krnlai config set api_key sk-or-...
```

## Iniciar uma Sessao Local

```bash
krnlai chat --local
```

A flag `--local` usa o kernel embutido com armazenamento SQLite. Nenhum servico externo necessario.

## Inicializar um Projeto

```bash
krnlai init my-agent
cd my-agent
krnlai run --interactive
```

Isso cria um scaffold de projeto com um template `CognitiveAgent`, um arquivo de politica e configuracao padrao.

## Proximos Passos

- Explore a [Referencia da CLI](../02-CLI/cli-reference.md)
- Entenda o [Ciclo Cognitivo](../04-Cognitive-Cycle/cognitive-cycle.md)
- Aprenda sobre o [Sistema de Seguranca](../06-Safety/safety-system.md)
- Construa com o [SDK](../08-SDK/sdk-guide.md)
