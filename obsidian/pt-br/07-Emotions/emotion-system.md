# Sistema Emocional

Krnl-AI inclui um modelo emocional que influencia a tomada de decisao com base no estado interno do agente. Isto e opcional e pode ser desabilitado.

## Modelo VAD

O estado emocional e modelado usando o modelo dimensional **Valencia-Excitacao-Dominancia (VAD)**:

| Dimensao | Faixa | Descricao |
|-----------|-------|-------------|
| **Valencia** | -1.0 a 1.0 | Prazer (positivo ↔ negativo) |
| **Excitacao** | -1.0 a 1.0 | Intensidade (calmo ↔ excitado) |
| **Dominancia** | -1.0 a 1.0 | Controle (submisso ↔ dominante) |

### Propriedades do Estado

```python
from krnlai import VADState

state = VADState(valence=0.5, arousal=0.2, dominance=0.3)
state.is_positive   # True se valence > 0
state.is_negative   # True se valence < 0
state.is_calm       # True se |arousal| < 0.3
state.is_intense    # True se |arousal| > 0.7
```

## Como as Emocoes Afetam o Comportamento

| Estado Emocional | Efeito |
|----------------|--------|
| **Valencia negativa** | Aumenta o risco percebido (vies ate +0.2) |
| **Alta excitacao** | Adiciona vies de risco (+0.1 por unidade) |
| **Valencia positiva** | Diminui ligeiramente o risco percebido |
| **Estado calmo** | Avaliacao neutra, sem vies |

## Transicoes Emocionais

O estado emocional muda com base em eventos durante o ciclo cognitivo:

| Evento | Efeito |
|-------|--------|
| Alto risco detectado | Mudanca de valencia negativa (-0.2), excitacao aumentada |
| Execucao bem-sucedida | Mudanca de valencia positiva (+0.05) |
| Decaimento natural | Retorno gradual ao neutro (5% por passo) |

```python
from krnlai.core.emotion.vad import VADModel

model = VADModel()
transition = model.update(
    delta_valence=-0.2,
    delta_arousal=0.3,
    trigger="alto_risco_detectado",
)
print(f"Anterior: {transition.previous_state}")
print(f"Atual: {transition.new_state}")
print(f"Delta: {transition.delta}")

# Decaimento emocional ao longo do tempo
model.decay(steps=3)
```

## Memoria Emocional

Todas as transicoes emocionais sao registradas e podem ser consultadas:

```python
# Timeline completa
model.history

# Pesquisar por gatilho
emotional_memory = model.emotional_memory  # se disponivel
emotional_memory.search_by_trigger("error")
```

## Modelo de Dor/Recompensa

Alem do modelo VAD, um sistema de dor/recompensa fornece sinais de aprendizado por reforco:

```python
from krnlai.core.emotion.pain_reward import PainRewardModel

pain_reward = PainRewardModel()
pain_reward.apply_many([
    {"type": "reward", "value": 0.5, "reason": "tarefa_concluida"},
    {"type": "pain", "value": -0.1, "reason": "alto_risco"},
])
```

## Configuracao

```python
agent = CognitiveAgent(enable_emotions=True)  # padrao
```

Quando desabilitado, o estado emocional e sempre neutro e nenhuma memoria emocional e registrada.
