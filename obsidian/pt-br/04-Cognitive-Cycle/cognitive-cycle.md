# Ciclo Cognitivo

O runtime cognitivo Krnl-AI implementa um ciclo de processamento cognitivo de 10 etapas inspirado na cognicao humana. Cada etapa processa a entrada atraves de uma funcao cognitiva diferente.

## As 10 Etapas

| # | Etapa | Descricao |
|---|-------|-----------|
| 1 | **Sensor** | Recebe e valida a entrada bruta |
| 2 | **Atencao** | Extrai caracteristicas e prioriza informacoes |
| 3 | **Memoria** | Recupera episodios relevantes, fatos semanticos e conhecimento procedural |
| 4 | **Avaliacao** | Verificacao de seguranca + pontuacao de risco + avaliacao de impacto emocional |
| 5 | **Metacognicao** | Auto-observa o estado emocional, nivel de risco e vieses cognitivos |
| 6 | **Planejamento** | Cria um plano de execucao com sub-etapas |
| 7 | **Governanca** | Validacao do motor de politicas contra politicas aprendidas |
| 8 | **Execucao** | Processa a acao atraves do conjunto de ferramentas permitidas |
| 9 | **Resultado** | Registra o resultado na memoria episodica |
| 10 | **Aprendizado** | Atualiza memoria semantica, politicas e estado emocional |

## Fases do Ciclo

O ciclo progride atraves de quatro fases de alto nivel:

```
PERCEPCAO → DELIBERACAO → ACAO → REFLEXAO
  (etapas 1-3)  (etapas 4-7)  (etapa 8)  (etapas 9-10)
```

## Ciclo Adaptativo

O kernel suporta um **loop adaptativo** que modula o comportamento com base na complexidade da tarefa e resultados anteriores. O loop adaptativo pode ajustar:

- **Profundidade do ciclo** — Processamento superficial (rapido) vs profundo (completo)
- **Esforco de planejamento** — Decomposicao heuristica para tarefas simples vs planejamento completo para tarefas complexas
- **Amplitude de recuperacao de memoria** — Estreita (apenas recente) vs ampla (busca temporal + analogica)
- **Supervisao metacognitiva** — Minima para tarefas rotineiras, maxima para situacoes de alto risco

## Ciclo Cognitivo de Codigo

Para tarefas de processamento de codigo, o kernel executa um **ciclo cognitivo de codigo especializado de 11 etapas**:

| # | Etapa | Descricao |
|---|-------|-----------|
| 1 | **CodeUnderstanding** | Analisa e entende o contexto do codigo |
| 2 | **IntentResolution** | Determina a intencao do usuario |
| 3 | **ImpactAnalysis** | Analisa o impacto das mudancas |
| 4 | **SafetyCheck** | Valida a seguranca do codigo |
| 5 | **DiffGeneration** | Gera diff de codigo |
| 6 | **AutoReview** | Auto-revisao do diff gerado |
| 7 | **RiskScoring** | Pontua o risco da mudanca |
| 8 | **Approval** | Requisita/auto-aprova |
| 9 | **Apply** | Aplica o diff |
| 10 | **Verify** | Verifica a mudanca |
| 11 | **Learning** | Aprende com o resultado |

## Fala Interna e Pensamento de Alta Ordem

Durante o ciclo, o kernel gera **fala interna** (narracao passo a passo do raciocinio) e **pensamentos de alta ordem** (autoconsciencia do estado cognitivo atual). Eles estao disponiveis para inspecao:

```python
async for event in agent.stream("analise estes dados"):
    print(f"[{event.step}] {event.narration}")
    if event.inner_speech:
        print(f"  Interno: {event.inner_speech}")
    if event.higher_order_thought:
        print(f"  Meta: {event.higher_order_thought}")
```

## Planejamento no Espaco Latente

Para tarefas complexas que envolvem simulacao de multiplos cenarios, o ciclo cognitivo pode utilizar **planejamento no espaco latente**. Isso permite que o kernel explore mentalmente diferentes sequencias de acoes antes de executa-las, usando modelos de mundo preditivos para avaliar resultados provaveis.

## Pipeline de Aprendizado Continuo

O ciclo inclui um **pipeline de aprendizado continuo** que orquestra varias funcoes cognitivas:

1. Recuperacao de memorias relevantes
2. Analise causal de resultados passados
3. Atualizacao do modelo de mundo com novas observacoes
4. Simulacao de cenarios offline (sonhos)
5. Consolidacao de novas informacoes na memoria

Este pipeline executa automaticamente em cada ciclo, permitindo que o agente melhore continuamente seu entendimento do ambiente.

## Streaming

Voce pode observar o ciclo em tempo real:

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
async for event in agent.stream("analise estes dados"):
    print(f"{event.step}: {event.status} ({event.duration_ms:.1f}ms)")
```

## Detalhes das Etapas

### 1. Sensor

Valida e armazena a entrada bruta no contexto da etapa. Verifica payloads vazios ou invalidos.

### 2. Atencao

Extrai caracteristicas da entrada:
- Comprimento e contagem de palavras
- Se a entrada contem uma pergunta
- Se a entrada comeca com um prefixo de comando (`/`, `!`, `run`)

### 3. Memoria

Recupera contexto relevante:
- Memorias episodicas recentes
- Fatos semanticos correspondentes a entrada
- Conhecimento procedural (procedimentos aprendidos)

### 4. Avaliacao

Executa o pipeline completo de seguranca contra a entrada:
- Verificacao de lista de permissao (apenas acoes `kernel.handle` permitidas)
- Pontuacao de risco
- Validacao de entrada
- Avaliacao de impacto emocional

### 5. Metacognicao

Auto-observa o estado atual:
- Alto risco detectado → flag de cautela
- Estado emocional negativo → conscientizacao
- Alta excitacao → vies de inibicao
- Deteccao de vies cognitivo

### 6. Planejamento

Cria um plano de execucao. Para tarefas simples: `analisar → executar → verificar`. Para tarefas complexas, usa decomposicao hierarquica com sub-objetivos e etapas paralelas.

### 7. Governanca

Aplica o motor de politicas para validar a acao planejada contra politicas aprendidas.

### 8. Execucao

Processa a entrada e produz saida.

### 9. Resultado

Registra o par completo de entrada/saida como uma entrada na memoria episodica.

### 10. Aprendizado

Atualiza a memoria semantica com novos fatos. Atualiza politicas com base no sucesso do resultado. Decai o estado emocional naturalmente.

## Configuracao

| Parametro | Padrao | Descricao |
|-----------|--------|-----------|
| `max_iterations` | 10 | Numero maximo de ciclos |
| `step_timeout_ms` | 5000 | Timeout por etapa |
| `cycle_timeout_ms` | 30000 | Timeout total do ciclo |
| `safety_level` | `strict` | `strict` ou `relaxed` |
| `enable_emotions` | `true` | Habilita modelo emocional |
| `enable_learning` | `true` | Habilita aprendizado de politicas |
| `enable_inner_speech` | `true` | Habilita narracao de fala interna |
