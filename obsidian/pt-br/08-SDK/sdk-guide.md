# Guia do SDK

Krnl-AI fornece SDKs tanto para Python quanto para .NET para construir aplicacoes de agente programaticamente.

## SDK Python

### Instalacao

```bash
pip install krnlai
```

### Inicio Rapido

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent(safety_level="strict")
response = await agent.run("analise este conjunto de dados")
print(response.output)
```

### API CognitiveAgent

```python
agent = CognitiveAgent(
    mode="auto",             # "auto" | "standalone" | "enterprise"
    safety_level="strict",   # "strict" | "relaxed"
    endpoint="",             # URL do backend C# (modo enterprise)
    api_key="",              # Chave de API do backend C#
    max_iterations=10,
    enable_emotions=True,
    enable_learning=True,
)
```

#### Metodos

| Metodo | Descricao |
|--------|-------------|
| `run(command)` | Executa um unico comando |
| `run_command(envelope)` | Executa um `CommandEnvelope` |
| `stream(command)` | Transmite eventos do ciclo cognitivo |
| `close()` | Limpa recursos |

### Modulos Disponiveis

| Modulo | Descricao |
|--------|-------------|
| `krnlai.llm.openai` | Provedor LLM OpenAI |
| `krnlai.llm.anthropic` | Provedor LLM Anthropic |
| `krnlai.llm.ollama` | Provedor LLM Ollama |
| `krnlai.llm.google` | Provedor Google Gemini |
| `krnlai.llm.deepseek` | Provedor DeepSeek |
| `krnlai.llm.groq` | Provedor Groq |
| `krnlai.llm.openrouter` | Provedor OpenRouter |
| `krnlai.core.safety` | Verificador de seguranca e regras |
| `krnlai.core.memory` | Memoria episodica, semantica, operacional, procedural, autobiografica, prospectiva |
| `krnlai.core.emotion` | Modelo emocional VAD + sistema de dor/recompensa |
| `krnlai.core.policies` | Mecanismo de politicas |
| `krnlai.core.risk` | Pontuacao de risco |
| `krnlai.core.cognition` | Ciclo cognitivo, metacognicao, fala interna, planejamento latente |
| `krnlai.core.consciousness` | Consciencia operacional, esquema de atencao, workspace global |
| `krnlai.core.world_model` | Modelos de mundo preditivos (baseados em JEPA) |
| `krnlai.core.causal` | Raciocinio causal baseado em grafos |
| `krnlai.core.active_inference` | Selecao de acoes baseada em energia livre |
| `krnlai.core.dream` | Simulacao e consolidacao de sonhos |
| `krnlai.core.continuous_learning` | Pipeline de aprendizado continuo |
| `krnlai.investigation` | Investigacao causal e analise de causa raiz |
| `krnlai.integrations` | LangChain, CrewAI, AutoGen, FastAPI |
| `krnlai.enterprise` | Cliente e armazenamentos empresariais |

| `krnlai.llm.mistral` | Provedor Mistral AI |
| `krnlai.llm.cohere` | Provedor Cohere |

### Provedores LLM

```python
from krnlai.llm.ollama import OllamaProvider

provider = OllamaProvider(model="llama3.1", endpoint="http://localhost:11434/v1")
response = await provider.chat("Ola!")
```

## SDK .NET

### Instalacao

```bash
dotnet add package KrnlAISdk
```

### Inicio Rapido

```csharp
using KrnlAI;

var client = new KrnlAIClient();
var response = await client.RunAsync("analise este conjunto de dados");
Console.WriteLine(response.Narration);
```

### Modelos

| Modelo | Descricao |
|-------|-------------|
| `AgentRunRequest` | Requisicao para executar o agente |
| `MemorySearchRequest` | Requisicao para pesquisar memoria |
| `EpisodeModels` | Modelos de dados de episodio |
| `HealthModels` | Modelos de verificacao de saude |
| `MetricsModels` | Modelos de metricas de performance |
| `GoalModels` | Modelos de rastreamento de metas |

### Uso

```csharp
// Pesquisar memoria
var memoryResult = await client.SearchMemoryAsync("decisao do projeto");

// Verificar saude
var health = await client.CheckHealthAsync();

// Obter metricas
var metrics = await client.GetMetricsAsync();
```
