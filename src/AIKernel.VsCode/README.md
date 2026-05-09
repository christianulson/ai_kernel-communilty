# AI Kernel — Extensão para VsCode

Extensão VsCode para interagir com o AI Kernel, um agente cognitivo que pode rodar **via API remota** ou **standalone local** com Sidecar .NET.

---

## 📦 Instalação

### Via VSIX (recomendado)

```bash
# 1. Compilar a extensão
cd src/AIKernel.VsCode
npm install
npx tsc

# 2. Empacotar
npx @vscode/vsce package

# 3. Instalar no VsCode
code --install-extension aikernel-vscode-1.0.0.vsix
```

### Via código fonte (desenvolvimento)

1. Abra a pasta raiz do projeto no VsCode
2. Pressione `F5` — abre o **Extension Development Host**
3. A extensão aparece na barra lateral com o ícone 🤖

---

## 🚀 Primeiros Passos

### 1. Abrir o Chat

- Clique no ícone **AI Kernel** na barra lateral (Activity Bar)
- Ou pressione `Ctrl+Shift+P` → `AI Kernel: Chat`
- Digite sua mensagem e pressione Enter

### 2. Escolher o Modo de Operação

A extensão funciona em **2 modos**:

| Modo | Configuração | Requer |
|------|-------------|--------|
| **API Remota** (padrão) | `aikernel.endpoint: "http://localhost:5000"` | Backend Gateway/Kernel rodando |
| **Standalone Local** | `aikernel.standalone: true` | .NET 10 SDK + Sidecar |

Para ativar o modo standalone:

```bash
# No terminal:
Ctrl+Shift+P → "AI Kernel: Iniciar Sidecar"

# Ou manualmente:
dotnet run --project src/AIKernel.Sidecar -- --port 5001
```

### 3. Configurar

`Ctrl+Shift+P` → `AI Kernel: Configurações`

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `aikernel.endpoint` | `http://localhost:5000` | URL do backend |
| `aikernel.standalone` | `false` | Usar Sidecar local |
| `aikernel.sidecarPort` | `5001` | Porta do Sidecar |

---

## 🖥️ Telas

### 💬 Chat
Interface de conversação com o agente. Envia prompts e recebe respostas narradas. Mostra status de conexão em tempo real.

### 📊 Dashboard
Scorecard de autonomia (5 dimensões: Confiabilidade, Eficiência, Safety, Anti-Loop, Governança) e saúde do sistema (Gateway + Kernel).

### 📋 Políticas
Lista de políticas aprendidas, com filtro por domínio (General, Payments, Security) e indicador de política ativa/inativa.

### 📜 Episódios
Histórico de execuções do agente com status, goal ID, duração. Clique em um episódio para ver detalhes e steps da execução.

### 🧠 Memória
Duas abas:
- **Busca** — pesquisa semântica com score de relevância
- **Métricas** — total de chunks, documentos e tamanho

### ⚙️ Configurações
Configure endpoint, modo standalone/remoto e porta do Sidecar.

---

## 🔧 Comandos

| Comando | Descrição |
|---------|-----------|
| `AI Kernel: Chat` | Abre o chat |
| `AI Kernel: Dashboard` | Abre o dashboard |
| `AI Kernel: Políticas` | Abre lista de políticas |
| `AI Kernel: Episódios` | Abre histórico |
| `AI Kernel: Memória` | Abre busca semântica |
| `AI Kernel: Configurações` | Abre configurações |
| `AI Kernel: Iniciar Sidecar` | Sobe processo Sidecar .NET |
| `AI Kernel: Parar Sidecar` | Mata processo Sidecar |

---

## 🏗️ Arquitetura

```
VsCode Extension (TypeScript)
│
├── Modo API Remota ── HTTP ──> Backend Gateway (:5000)
│                                 └── Kernel API
│
└── Modo Standalone ── spawn ──> AIKernel.Sidecar.exe (:5001)
                                   └── Kernel.Core (in-process)
                                       ├── AdversarialGuard
                                       ├── SimpleRiskScorer
                                       ├── MonteCarloTreeSearch
                                       └── InMemory stores
```

---

## 🔄 Requisitos

- **VsCode** 1.85+
- **.NET 10 SDK** (para modo standalone)
- **Node.js** 18+ (para desenvolvimento)

## 📄 Licença

MIT
