# Configuracao

## Configuracao de Provedor

Configure seu provedor LLM via CLI:

```bash
krnlai config set provider <nome>
krnlai config set api_key <chave>
krnlai config set endpoint <url>
krnlai config set model <nome-do-modelo>
```

Listar todas as configuracoes atuais:

```bash
krnlai config list
```

## Provedores Suportados

| Provedor | Nome de Config | Requer Chave | Requer Endpoint |
|----------|-------------|-------------|-------------------|
| OpenAI | `openai` | Sim | Nao |
| Ollama | `ollama` | Nao | Sim |
| Anthropic | `anthropic` | Sim | Nao |
| OpenRouter | `openrouter` | Sim | Nao |
| DeepSeek | `deepseek` | Sim | Nao |
| Google Gemini | `google` | Sim | Nao |
| Groq | `groq` | Sim | Nao |
| Compativel OpenAI | `openai-compatible` | Opcional | Sim |

## Modo Sidecar

O sidecar tem dois modos:

- **Legacy** (padrao) — Pipeline de seguranca completo com guarda adversarial, regras fundamentais e aplicador etico
- **Community** — Endpoints simplificados para integracoes leves

Defina via `appsettings.json`:

```json
{
  "Sidecar": {
    "Mode": "Community"
  }
}
```

## Configuracao de Armazenamento

O kernel embutido suporta varios backends de armazenamento:

| Configuracao | Opcoes | Padrao |
|---------|---------|---------|
| `Store:Mode` | `SQLite` | `SQLite` |
| `Store:SqliteMode` | `Hybrid`, `Memory` | `Hybrid` |
| `Vector:Mode` | `Sqlite`, `Memory` | `Sqlite` |
| `Cache:Mode` | `Memory`, `None` | `Memory` |
| `Skills:StoreMode` | `Document`, `Memory` | `Document` |
