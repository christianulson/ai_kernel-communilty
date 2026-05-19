# Referencia da CLI

A CLI do Krnl-AI (`krnlai`) e a interface principal para interagir com o runtime comunitario.

## Comandos Principais

### `krnlai chat --local`

Inicie uma sessao de chat TUI interativa com o kernel local embutido.

```bash
krnlai chat --local
krnlai chat --local --model llama3.1
```

### `krnlai init <nome>`

Crie um novo projeto de agente com arquivos de scaffold.

```bash
krnlai init my-agent
```

### `krnlai run <texto>`

Execute o agente uma vez com o texto de entrada fornecido.

```bash
krnlai run "analise este conjunto de dados"
krnlai run --interactive
```

### `krnlai serve --local --port <porta>`

Inicie o servidor HTTP sidecar para integracoes com editores e automacao.

```bash
krnlai serve --local --port 5117
```

## Comandos de Memoria

### `krnlai memory search <consulta>`

Pesquise na memoria semantica por documentos relevantes.

```bash
krnlai memory search "decisao do projeto"
```

### `krnlai memory snapshot`

Crie um snapshot do estado atual da memoria.

```bash
krnlai memory snapshot
```

### `krnlai memory metrics`

Veja estatisticas de uso da memoria.

```bash
krnlai memory metrics
```

## Comandos de Seguranca

### `krnlai safety run`

Execute verificacoes de seguranca contra a configuracao atual.

```bash
krnlai safety run
```

### `krnlai safety status`

Exiba o status de todas as camadas de seguranca.

```bash
krnlai safety status
```

### `krnlai security audit`

Execute uma auditoria de seguranca completa do sistema de seguranca.

```bash
krnlai security audit
```

### `krnlai security benchmark <contagem>`

Benchmark de performance do sistema de seguranca (padrao: 1000 iteracoes).

```bash
krnlai security benchmark 5000
```

### `krnlai security report <arquivo>`

Gere um relatorio de seguranca HTML.

```bash
krnlai security report report.html
```

## Comandos de Habilidade

### `krnlai skill list`

Liste habilidades instaladas.

```bash
krnlai skill list
```

### `krnlai skill export <nome> <arquivo>`

Exporte uma habilidade para compartilhamento.

```bash
krnlai skill export my-skill skill.json
```

### `krnlai skill import <arquivo>`

Importe uma habilidade de um arquivo.

```bash
krnlai skill import skill.json
```

## Comandos de Politica

### `krnlai policy list`

Liste politicas aprendidas.

```bash
krnlai policy list
```

### `krnlai policy show <id>`

Mostre detalhes de uma politica especifica.

```bash
krnlai policy show policy-1
```

## Comandos de Sessao

### `krnlai session list`

Liste todas as sessoes.

```bash
krnlai session list
```

### `krnlai session export <id> <arquivo>`

Exporte uma sessao para compartilhamento ou analise.

```bash
krnlai session export session-1 session.json
```

## Comandos de Utilidade

### `krnlai config list`

Mostre todos os valores de configuracao.

```bash
krnlai config list
```

### `krnlai config set <chave> <valor>`

Defina um valor de configuracao.

```bash
krnlai config set model llama3.1
```

### `krnlai status`

Mostre o status atual do runtime Krnl-AI.

```bash
krnlai status
```

### `krnlai health`

Verifique a saude do runtime e dos provedores configurados.

```bash
krnlai health
```

### `krnlai upgrade`

Atualize a CLI para a versao mais recente.

```bash
krnlai upgrade
```

## Comandos de Template

### `krnlai templates list`

Liste templates de projeto disponiveis.

```bash
krnlai templates list
```

### `krnlai new <template> <nome>`

Crie um novo projeto a partir de um template.

```bash
krnlai new agent my-agent
krnlai new tool my-tool
krnlai new policy my-policy
```
