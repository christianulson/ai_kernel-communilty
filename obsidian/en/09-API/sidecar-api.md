# Sidecar API

The Krnl-AI Sidecar is a local HTTP server that exposes the cognitive runtime for editor integrations, desktop apps, automation, and peer-to-peer signaling.

## Starting the Sidecar

```bash
krnlai serve --local --port 5117
```

By default, the sidecar starts on `http://127.0.0.1:5001`.

## Endpoints

### Health

```http
GET /health
```

Response:
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

### Readiness Check

```http
GET /health/ready
GET /health/live
```

### Agent Run

```http
POST /agent/run
Content-Type: application/json

{
  "prompt": "analyze this data",
  "mode": "standalone"
}
```

Response:
```json
{
  "narration": "Processed in standalone mode.",
  "error": null,
  "transportSteps": [
    {"label": "AdversarialGuard", "detail": "OK", "ok": true},
    {"label": "FundamentalRules", "detail": "OK", "ok": true},
    {"label": "EthicalEnforcer", "detail": "OK", "ok": true}
  ],
  "activeStages": ["standalone"]
}
```

### P2P / WebRTC Signaling

The desktop client uses a WebSocket signaling endpoint at `/signaling/webrtc` to bootstrap peer-to-peer calls.

```text
ws://127.0.0.1:5001/signaling/webrtc
```

Supported signaling messages:

- `join`
- `offer`
- `answer`
- `leave`
- `audio`
- `video`

Desktop clients use the signaling channel to exchange offers and session metadata while audio/video media remains peer-to-peer. STUN/TURN values are configured from the desktop settings surface.

### Memory Search

```http
POST /memory/search
Content-Type: application/json

{
  "query": "project decision",
  "limit": 10,
  "offset": 0,
  "domain": ""
}
```

Response:
```json
{
  "hits": [],
  "totalCount": 0
}
```

### Memory Metrics

```http
GET /memory/metrics
```

Response:
```json
{
  "totalChunks": 0,
  "totalDocuments": 0,
  "totalSizeBytes": 0
}
```

### Episode Search

```http
GET /episodes/search
```

### Episode by ID

```http
GET /episodes/{id}
```

### Policy List

```http
GET /policy/list
```

### Agent Metrics Scorecard

```http
GET /agent/metrics/scorecard
```

Response:
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

## Rate Limiting

The sidecar applies per-endpoint rate limiting:

| Endpoint | Limit | Window |
|----------|-------|--------|
| `/agent/run` | 10 requests | 10 seconds |
| `/memory/search` | 30 requests | 10 seconds |
| Global | 60 requests | 10 seconds |

## Community Mode

When running in community mode (`Sidecar:Mode = Community`), the sidecar exposes simplified endpoints without the full C# safety pipeline dependency:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/agent/run` | POST | Run agent via embedded kernel |
| `/memory/search` | POST | Search local memory |
| `/health` | GET | Health status |

## Configuration

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
