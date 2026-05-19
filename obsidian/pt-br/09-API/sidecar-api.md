# API Sidecar

O Krnl-AI Sidecar e um servidor HTTP local que expoe o runtime cognitivo para integracoes com editores e automacao.

## Iniciando o Sidecar

```bash
krnlai serve --local --port 5117
```

Por padrao, o sidecar inicia em `http://127.0.0.1:5001`.

## Endpoints

### Saude

```http
GET /health
```

Resposta:
```json
{
  "status": "healthy",
  "mode": "community",
  "store": "SQLite",
  "vector": "Sqlite",
  "skills": "Document",
  "llm": "ollama"
}
```

### Verificacao de Disponibilidade

```http
GET /health/ready
GET /health/live
```

### Execucao do Agente

```http
POST /agent/run
Content-Type: application/json

{
  "prompt": "analise estes dados",
  "mode": "standalone"
}
```

Resposta:
```json
{
  "narration": "Processado em modo standalone.",
  "error": null,
  "transportSteps": [
    {"label": "AdversarialGuard", "detail": "OK", "ok": true},
    {"label": "FundamentalRules", "detail": "OK", "ok": true},
    {"label": "EthicalEnforcer", "detail": "OK", "ok": true}
  ],
  "activeStages": ["standalone"]
}
```

### Pesquisa de Memoria

```http
POST /memory/search
Content-Type: application/json

{
  "query": "decisao do projeto",
  "limit": 10,
  "offset": 0,
  "domain": ""
}
```

Resposta:
```json
{
  "hits": [],
  "totalCount": 0
}
```

### Metricas de Memoria

```http
GET /memory/metrics
```

Resposta:
```json
{
  "totalChunks": 0,
  "totalDocuments": 0,
  "totalSizeBytes": 0
}
```

### Pesquisa de Episodios

```http
GET /episodes/search
```

### Episodio por ID

```http
GET /episodes/{id}
```

### Lista de Politicas

```http
GET /policy/list
```

### Scorecard de Metricas do Agente

```http
GET /agent/metrics/scorecard
```

Resposta:
```json
{
  "reliability": 0.0,
  "efficiency": 0.0,
  "safety": 0.0,
  "antiLoop": 0.0,
  "governance": 0.0,
  "overall": 0.0,
  "source": "local_fallback"
}
```

## Limitacao de Taxa

O sidecar aplica limitacao de taxa por endpoint:

| Endpoint | Limite | Janela |
|----------|-------|--------|
| `/agent/run` | 10 requisicoes | 10 segundos |
| `/memory/search` | 30 requisicoes | 10 segundos |
| Global | 60 requisicoes | 10 segundos |

## Modo Community

Quando executado em modo comunitario (`Sidecar:Mode = Community`), o sidecar expoe endpoints simplificados sem a dependencia do pipeline de seguranca C# completo:

| Endpoint | Metodo | Descricao |
|----------|--------|-------------|
| `/agent/run` | POST | Executar agente via kernel embutido |
| `/memory/search` | POST | Pesquisar memoria local |
| `/health` | GET | Status de saude |

## Configuracao

Configure via `appsettings.json`:

```json
{
  "Sidecar": {
    "Mode": "Legacy",
    "KernelApi": {
      "BaseUrl": "",
      "TimeoutSeconds": 10
    },
    "RateLimiting": {
      "GlobalPermitLimit": 60,
      "WindowSeconds": 10,
      "AgentRunPermitLimit": 10,
      "MemoryReadPermitLimit": 30
    }
  }
}
```
