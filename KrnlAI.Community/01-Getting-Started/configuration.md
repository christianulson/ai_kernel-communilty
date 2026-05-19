# Configuration

## Provider Configuration

Configure your LLM provider via the CLI:

```bash
krnlai config set provider <name>
krnlai config set api_key <key>
krnlai config set endpoint <url>
krnlai config set model <model-name>
```

List all current settings:

```bash
krnlai config list
```

## Supported Providers

| Provider | Config Name | Requires Key | Requires Endpoint |
|----------|-------------|-------------|-------------------|
| OpenAI | `openai` | Yes | No |
| Ollama | `ollama` | No | Yes |
| Anthropic | `anthropic` | Yes | No |
| OpenRouter | `openrouter` | Yes | No |
| DeepSeek | `deepseek` | Yes | No |
| Google Gemini | `google` | Yes | No |
| Groq | `groq` | Yes | No |
| OpenAI-Compatible | `openai-compatible` | Optional | Yes |

## Sidecar Mode

The sidecar has two modes:

- **Legacy** (default) — Full safety pipeline with adversarial guard, fundamental rules, and ethical enforcer
- **Community** — Simplified endpoints for lightweight integrations

Set via `appsettings.json`:

```json
{
  "Sidecar": {
    "Mode": "Community"
  }
}
```

## Store Configuration

The embedded kernel supports multiple storage backends:

| Setting | Options | Default |
|---------|---------|---------|
| `Store:Mode` | `SQLite` | `SQLite` |
| `Store:SqliteMode` | `Hybrid`, `Memory` | `Hybrid` |
| `Vector:Mode` | `Sqlite`, `Memory` | `Sqlite` |
| `Cache:Mode` | `Memory`, `None` | `Memory` |
| `Skills:StoreMode` | `Document`, `Memory` | `Document` |
