# Extensoes de Editor

Krnl-AI se integra com editores de codigo populares para trazer capacidades de agente cognitivo diretamente para seu fluxo de desenvolvimento.

## Extensao VS Code

Uma extensao baseada em TypeScript que adiciona recursos do Krnl-AI ao VS Code.

### Recursos

- **Painel de Chat** — Interaja com o agente diretamente no VS Code
- **Completacoes Embutidas** — Sugestoes de codigo alimentadas por IA
- **Acoes de Codigo** — Refatore, explique e gere codigo
- **Agente de Codificacao** — Loop agentico autonomo para tarefas complexas
- **Integracao de Memoria** — Pesquise e navegue pela memoria do agente
- **Visualizador de Episodios** — Navegue pelo historico de execucao
- **Visualizador de Politicas** — Veja e gerencie politicas
- **Integracao Git** — Revisao automatica, mensagens de commit, descricoes de PR
- **Teletransporte de Sessao** — Persista e restaure sessoes

### Comandos de Chat

| Comando | Descricao |
|---------|-------------|
| `Ctrl+Shift+P` → `KrnlAI: Chat` | Abrir painel de chat |
| `Ctrl+Shift+P` → `KrnlAI: Inline` | Solicitar completacao embutida |

### Participantes de Chat

A extensao inclui um participante de chat `@krnlai` para a interface de chat nativa do VS Code:

```
@krnlai explique esta funcao
@krnlai revise minhas alteracoes
@krnlai gere testes para esta classe
```

### Painel de Dashboard

Fornece metricas em tempo real e status do runtime cognitivo.

### Instalacao

```bash
# Do VS Code Marketplace (quando publicado)
ext install KrnlAI.VsCode

# Da fonte
cd src/KrnlAI.VsCode
npm install
npm run compile
```

## Extensao Visual Studio

Uma extensao baseada em .NET para Visual Studio 2022.

### Recursos

- **Janela de Ferramenta** — Painel dedicado do Krnl-AI
- **Enviar Selecao para Chat** — Envie codigo selecionado para o agente
- **Analisar Erro** — Obtenha analise de erro alimentada por IA
- **Historico de Chat** — Historico de conversacao persistente

### Comandos da Janela de Ferramenta

| Comando | Descricao |
|---------|-------------|
| `Exibir → Outras Janelas → Krnl-AI` | Abrir a janela de ferramenta Krnl-AI |
| Clique direito → `Enviar para Krnl-AI` | Enviar codigo selecionado para chat |
| Clique direito no erro → `Analisar com Krnl-AI` | Analisar erro de compilacao |

### Configuracao

Configuracoes disponiveis via `Ferramentas → Opcoes → Krnl-AI`:

- URL do endpoint sidecar
- Provedor padrao
- Nivel de seguranca
- Preferencia de tema

### Instalacao

Compile e instale o VSIX de `src/KrnlAI.VisualStudio/`:

```bash
cd src/KrnlAI.VisualStudio
dotnet build
# Instale bin/Debug/KrnlAI.VisualStudio.vsix
```
