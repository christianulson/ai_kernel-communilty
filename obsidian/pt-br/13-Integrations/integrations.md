# Integracoes

Krnl-AI se integra com frameworks e ferramentas de IA populares.

## LangChain

```python
from krnlai.integrations.langchain import KrnlAITool

tool = KrnlAITool(safety_level="strict")
# Use com agentes e chains do LangChain
```

## CrewAI

```python
from krnlai.integrations.crewai import KrnlAIAgent

agent = KrnlAIAgent(safety_level="strict")
# Use em equipes e tarefas do CrewAI
```

## AutoGen

```python
from krnlai.integrations.autogen import KrnlAIAssistantAgent

agent = KrnlAIAssistantAgent(name="krnlai", safety_level="strict")
# Use em conversas multi-agente do AutoGen
```

## FastAPI

```python
from fastapi import FastAPI
from krnlai.integrations.fastapi import KrnlAIMiddleware

app = FastAPI()
app.add_middleware(KrnlAIMiddleware)
# Todas as requisicoes agora passam pelas verificacoes de seguranca do Krnl-AI
```

## Modo Enterprise

Conecte o SDK Python ao backend empresarial C# para persistencia e escala:

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent(
    mode="enterprise",
    endpoint="http://kernel-api:5001",
    api_key="sk-...",
)
```

### Deteccao Automatica

```python
# Detecta automaticamente o backend C#, volta para standalone
agent = CognitiveAgent(mode="auto")
```

### Componentes Enterprise

| Componente | Descricao |
|-----------|-------------|
| `EnterpriseClient` | Cliente HTTP/gRPC para backend C# |
| `StreamingClient` | Streaming SSE via gerador assincrono |
| `QdrantStore` | Armazenamento vetorial para memoria semantica |
| `MySQLStore` | Armazenamento persistente para ciclos e trilhas de auditoria |
