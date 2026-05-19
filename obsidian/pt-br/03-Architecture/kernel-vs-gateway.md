# Kernel vs Gateway

A arquitetura segue um principio de **separacao de poderes**: o kernel toma decisoes e gerencia estado, enquanto o LLM traduz linguagem natural em acoes estruturadas.

## A Regra de Ouro

> **O LLM nunca decide ou escreve estado diretamente. Ele apenas traduz.**

## Responsabilidades do Kernel

| Aspecto | Descricao |
|--------|-------------|
| **Estado** | Gerencia todo o estado persistente e operacional |
| **Memoria** | Memoria episodica, semantica, operacional e emocional |
| **Seguranca** | Avalia acoes contra 20 regras fundamentais |
| **Politicas** | Armazena e aplica politicas aprendidas |
| **Avaliacao** | Avalia sinais e calcula pontuacoes de risco |
| **Aprendizado** | Atualiza politicas e memoria semantica a partir de resultados |

## Responsabilidades do Gateway (quando proxy para enterprise)

| Aspecto | Descricao |
|--------|-------------|
| **Traducao** | Converte linguagem natural em comandos estruturados |
| **Planejamento** | Divide objetivos em planos de execucao |
| **Narracao** | Converte resultados de volta em texto legivel |
| **Coordenacao** | Orquestra o fluxo plano-seguranca-execucao |

## Community vs Enterprise

| Recurso | Community (Local) | Enterprise (Proxy) |
|---------|------------------|---------------------|
| Armazenamento | SQLite | MySQL + Qdrant |
| Vetores | Armazenamento vetorial SQLite | Qdrant HNSW |
| Cache | Em memoria | Redis |
| Seguranca | Pipeline completo | Pipeline completo + MetaCritic |
| Autenticacao | Nenhum (localhost) | JWT + RBAC |
| Escala | Usuario unico | Multi-tenant |
