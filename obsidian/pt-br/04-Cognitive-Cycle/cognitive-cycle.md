# Ciclo Cognitivo

O runtime cognitivo Krnl-AI implementa um ciclo de processamento cognitivo de 10 etapas inspirado na cognicao humana. Cada etapa processa a entrada atraves de uma funcao cognitiva diferente.

## As 10 Etapas

| # | Etapa | Descricao |
|---|------|-------------|
| 1 | **Sensor** | Recebe e valida a entrada bruta |
| 2 | **Atencao** | Extrai caracteristicas e prioriza informacoes |
| 3 | **Memoria** | Recupera episodios relevantes e fatos semanticos |
| 4 | **Avaliacao** | Verificacao de seguranca + pontuacao de risco |
| 5 | **Metacognicao** | Auto-observa o estado emocional e nivel de risco |
| 6 | **Planejamento** | Cria um plano de execucao |
| 7 | **Governanca** | Validacao do mecanismo de politicas |
| 8 | **Execucao** | Processa a acao |
| 9 | **Resultado** | Registra o resultado na memoria episodica |
| 10 | **Aprendizado** | Atualiza memoria semantica e estado emocional |

## Fases do Ciclo

O ciclo progride atraves de quatro fases de alto nivel:

```
PERCEPCAO → DELIBERACAO → ACAO → REFLEXAO
  (etapas 1-3)  (etapas 4-7)  (etapa 8)  (etapas 9-10)
```

## Iteracoes

O ciclo cognitivo executa em iteracoes (maximo padrao: 10). Cada iteracao processa todas as 10 etapas. Se ocorrerem erros no modo de seguranca rigoroso, o ciclo para imediatamente.

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

Valida e armazena a entrada bruta no contexto da etapa. Verifica cargas vazias ou invalidas.

### 2. Atencao

Extrai caracteristicas da entrada:
- Tamanho e contagem de palavras
- Se a entrada contem uma pergunta
- Se a entrada comeca com um prefixo de comando (`/`, `!`, `run`)

### 3. Memoria

Recupera contexto relevante:
- Ultimas 3 memorias episodicas
- Fatos semanticos correspondentes a entrada
- Armazena entrada na memoria operacional

### 4. Avaliacao

Executa o pipeline de seguranca completo contra a entrada:
- Verificacao de lista de permissoes (apenas acoes `kernel.handle` permitidas)
- Pontuacao de risco
- Validacao de entrada
- Avaliacao de impacto emocional

### 5. Metacognicao

Auto-observa o estado atual:
- Alto risco detectado → flag de cautela
- Estado emocional negativo → conscientizacao
- Alta excitacao → vies de inibicao

### 6. Planejamento

Cria um plano simples de 3 etapas: `analisar → executar → verificar`.

### 7. Governanca

Aplica o mecanismo de politicas para validar a acao planejada contra politicas aprendidas.

### 8. Execucao

Processa a entrada e produz saida.

### 9. Resultado

Registra o par completo entrada/saida como uma entrada de memoria episodica.

### 10. Aprendizado

Atualiza a memoria semantica com um novo fato sobre a entrada processada. Decai o estado emocional naturalmente.

## Configuracao

| Parametro | Padrao | Descricao |
|-----------|---------|-------------|
| `max_iterations` | 10 | Numero maximo de ciclos |
| `step_timeout_ms` | 5000 | Timeout por etapa |
| `cycle_timeout_ms` | 30000 | Timeout total do ciclo |
| `safety_level` | `strict` | `strict` ou `relaxed` |
| `enable_emotions` | `true` | Habilitar modelo emocional |
| `enable_learning` | `true` | Habilitar aprendizado de politicas |
