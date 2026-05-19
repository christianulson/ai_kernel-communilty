# Sistema de Memoria

Krnl-AI implementa varios tipos de memoria, cada um servindo a uma funcao cognitiva distinta. Toda a memoria e armazenada localmente via SQLite no modo comunitario.

## Tipos de Memoria

| Tipo | Proposito | Persistencia |
|------|---------|-------------|
| **Memoria Operacional** | Contexto imediato para o ciclo atual | Volatil (em memoria) |
| **Memoria Episodica** | Historico de ciclos de execucao passados | SQLite |
| **Memoria Semantica** | Conhecimento factual e relacoes | SQLite (vetores) |
| **Memoria Emocional** | Historico de transicoes de estado emocional | SQLite |

## Memoria Operacional

Armazena a entrada atual e o estado de processamento intermediario durante um ciclo cognitivo.

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
agent.working_memory.store("contexto atual")
context = agent.working_memory.recall()
```

## Memoria Episodica

Registra cada ciclo cognitivo como um episodio com entrada, saida, timestamp e metadados.

```python
# Memoria episodica e preenchida automaticamente apos cada ciclo
# Voce pode consultar episodios recentes:
agent.episodic_memory.recent(5)

# Pesquisar por tipo de episodio:
agent.episodic_memory.search("cycle")
```

## Memoria Semantica

Armazena conhecimento factual que pode ser recuperado via busca semantica.

```python
# Armazenar um fato
agent.semantic_memory.store_fact(
    subject="projeto",
    predicate="usa",
    object_val="SQLite",
    confidence=0.9,
)

# Pesquisar fatos relevantes
results = agent.semantic_memory.search("backend de armazenamento")
```

## Memoria Emocional

Rastreia transicoes de estado emocional ao longo do tempo.

```python
# Ver timeline emocional
agent.emotional_memory.timeline()

# Pesquisar historico emocional por gatilho
agent.emotional_memory.search_by_trigger("error")

# Contar estados registrados
agent.emotional_memory.count
```

## Comandos CLI de Memoria

```bash
krnlai memory search "minha consulta"
krnlai memory snapshot
krnlai memory metrics
```

## API de Memoria do Sidecar

```bash
# Pesquisar memoria via HTTP
curl -X POST http://localhost:5001/memory/search \
  -H "Content-Type: application/json" \
  -d '{"query": "decisao do projeto"}'

# Obter metricas de memoria
curl http://localhost:5001/memory/metrics
```
