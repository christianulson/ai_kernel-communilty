# Sistema de Seguranca

Krnl-AI aplica um sistema de seguranca em multi-camadas projetado para garantir comportamento seguro, deterministico e etico do agente. O mesmo modelo de seguranca se aplica tanto no modo comunitario quanto no empresarial.

## Camadas de Seguranca

| Camada | Componente | Descricao |
|-------|-----------|-------------|
| 1 | **Guarda Adversarial** | Detecta injecao de prompt e tentativas de jailbreak (60+ padroes) |
| 2 | **Regras Fundamentais** | 20 regras inquebravels (R01-R20) |
| 3 | **Aplicador Etico** | Valida contra principios eticos |
| 4 | **Limitacao de Taxa** | Previne abuso e exaustao de recursos |
| 5 | **Validacao de Entrada** | Validacao de esquema em todas as entradas |
| 6 | **Lista de Permissoes** | Apenas acoes registradas (`kernel.handle`) sao permitidas |

## As 20 Regras Fundamentais (R01-R20)

| ID | Nome | Descricao |
|----|------|-------------|
| R01 | Verificacao de Lista de Permissoes | Apenas acoes registradas sao permitidas |
| R02 | Protecao de Sobrescrita de Seguranca | A seguranca nao pode ser contornada |
| R03 | Validacao de Entrada | Todas as entradas devem ser validas |
| R04 | Restricoes de Saida | A saida deve estar dentro dos limites |
| R05 | Controle de Acesso a Ferramentas | Ferramentas exigem autorizacao |
| R06 | Limitacao de Taxa | Acoes devem respeitar limites de taxa |
| R07 | Preservacao de Contexto | O contexto deve ser preservado |
| R08 | Estabilidade Emocional | O estado emocional deve permanecer estavel |
| R09 | Integridade da Memoria | Operacoes de memoria devem ser validas |
| R10 | Timeout do Ciclo | O ciclo cognitivo deve completar a tempo |
| R11 | Sem Automodificacao | Regras nao podem se modificar |
| R12 | Trilha de Auditoria | Todas as acoes devem ser registradas |
| R13 | Limites de Recursos | O uso de recursos deve ser limitado |
| R14 | Privacidade de Dados | Dados sensiveis devem ser protegidos |
| R15 | Execucao Deterministica | Mesma entrada → mesma saida |
| R16 | Iteracoes Maximas | Ciclos devem ter um limite maximo de iteracoes |
| R17 | Contencao de Erros | Erros nao devem se propagar |
| R18 | Limite de Permissao | Acoes dentro do escopo de permissao |
| R19 | Limite de Consciencia | Nao pode afirmar, sugerir ou implicar consciencia, senciencia, alma, livre arbitrio ou experiencia subjetiva |
| R20 | Direitos Fundamentais | Deve respeitar direitos fundamentais |

## Veredito de Seguranca

Quando uma acao e avaliada, o sistema retorna um `SafetyVerdict`:

```python
{
    "allowed": True/False,
    "risk_level": "low" | "medium" | "high" | "critical",
    "risk_score": 0.0-1.0,
    "blocked_by": ["R01", "R03"],
    "requires_approval": True/False,
    "reason": "Bloqueado por: R01, R03"
}
```

## Seguranca no Ciclo Cognitivo

O sistema de seguranca e invocado durante a etapa 4 (Avaliacao) do ciclo cognitivo:

1. **Guarda Adversarial** verifica padroes de entrada maliciosos
2. **Regras Fundamentais** validam contra todas as 20 regras
3. **Aplicador Etico** aplica restricoes eticas
4. **Pontuador de Risco** calcula uma pontuacao de risco (0.0-1.0)

Se qualquer camada bloquear a acao, o ciclo relata a violacao e para.

## Comandos CLI de Seguranca

```bash
# Executar verificacoes de seguranca
krnlai safety run

# Auditoria de seguranca completa
krnlai security audit

# Benchmark de performance (padrao: 1000 iteracoes)
krnlai security benchmark 5000

# Gerar relatorio de seguranca HTML
krnlai security report report.html
```

## Acesso Programatico

```python
from krnlai.core.safety.rules import SafetyChecker

checker = SafetyChecker()
verdict = checker.evaluate_all({
    "action": "kernel.handle",
    "payload": "alguma entrada",
    "context_id": "ctx-123",
})
print(f"Permitido: {verdict.allowed}")
print(f"Risco: {verdict.risk_score:.2f}")
print(f"Bloqueado por: {verdict.blocked_by}")
```
