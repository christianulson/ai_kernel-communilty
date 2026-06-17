# Sistema de Memoria

Krnl-AI implementa varios tipos de memoria modelados a partir da cognicao humana. Toda a memoria e armazenada localmente via SQLite no modo comunitario.

## Tipos de Memoria

| Tipo | Proposito | Persistencia |
|------|-----------|-------------|
| **Memoria Operacional** | Contexto imediato para o ciclo atual | Volatil (em memoria) |
| **Memoria Episodica** | Historico de ciclos de execucao passados | SQLite |
| **Memoria Semantica** | Conhecimento factual e relacoes | SQLite (vetores) |
| **Memoria Procedural** | Procedimentos e habilidades aprendidas | SQLite |
| **Memoria Emocional** | Historico de transicoes de estado emocional | SQLite |
| **Memoria Autobiografica** | Narrativa do proprio historico do agente | SQLite |
| **Memoria Prospectiva** | Intencoes futuras e metas pendentes | SQLite |

## Memoria Operacional

Armazena a entrada atual e o estado de processamento intermediario durante um ciclo cognitivo. Inclui filtragem baseada em atencao e expiracao por tempo de vida.

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
agent.working_memory.store("contexto atual")
context = agent.working_memory.recall()
```

## Memoria Episodica

Registra cada ciclo cognitivo como um episodio com entrada, saida, timestamp e metadados. Suporta busca temporal e recordacao associativa.

```python
# Memoria episodica e preenchida automaticamente apos cada ciclo
# Voce pode consultar episodios recentes:
agent.episodic_memory.recent(5)

# Pesquisar por tipo de episodio:
agent.episodic_memory.search("cycle")

# Busca temporal em um intervalo de tempo:
agent.episodic_memory.search_temporal("cycle", since="2026-01-01")
```

## Memoria Semantica

Armazena conhecimento factual como triplas sujeito-predicado-objeto com pontuacoes de confianca. Suporta busca semantica baseada em vetores.

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

## Memoria Procedural

Armazena procedimentos aprendidos — sequencias de acoes que foram bem-sucedidas no passado. Procedimentos sao extraidos automaticamente de execucoes de plano bem-sucedidas.

```python
# Listar procedimentos aprendidos
agent.procedural_memory.list()

# Pesquisar procedimentos por padrao de gatilho
agent.procedural_memory.search("analise de dados")

# Aplicar um procedimento
agent.procedural_memory.apply("analyze_dataset")
```

## Memoria Autobiografica

Mantem uma narrativa do proprio historico do agente, ligando episodios relacionados em historias coerentes. Usado para autorreflexao e continuidade de identidade.

```python
# Obter resumo narrativo
agent.autobiographical_memory.narrative()

# Pesquisar por periodo de vida
agent.autobiographical_memory.search_by_period("ultima_semana")
```

## Memoria Prospectiva

Armazena intencoes futuras e metas pendentes. O agente verifica periodicamente a memoria prospectiva para agir em intencoes adiadas.

```python
# Definir uma intencao futura
agent.prospective_memory.remember(
    intention="enviar relatorio semanal",
    trigger="sexta 17h",
)

# Verificar intencoes pendentes
agent.prospective_memory.pending()
```

## Memoria Emocional

Rastreia transicoes de estado emocional ao longo do tempo com snapshots VAD (Valencia-Excitação-Dominancia) e anotacoes de gatilho.

```python
# Ver timeline emocional
agent.emotional_memory.timeline()

# Pesquisar historico emocional por gatilho
agent.emotional_memory.search_by_trigger("error")

# Contar estados registrados
agent.emotional_memory.count
```

## Ranqueamento de Memoria

Ao recuperar informacoes, o kernel classifica itens de memoria usando multiplos sinais:

- **Recencia** — Quao recentemente a memoria foi acessada
- **Relevancia** — Similaridade semantica com o contexto atual
- **Saliencia emocional** — Impacto emocional da memoria
- **Padrao temporal** — Padroes sazonais ou ciclicos
- **Correspondencia analogica** — Similaridade com situacoes passadas
- **Atencao neural** — Pontuacao de relevancia aprendida por rede neural

## Comandos CLI de Memoria

```bash
# Pesquisar memoria
krnlai memory search "minha consulta"

# Tirar um snapshot da memoria
krnlai memory snapshot

# Ver metricas de memoria
krnlai memory metrics

# Listar procedimentos
krnlai skill list
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
