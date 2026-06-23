# Debug da Extensão VsCode — Krnl-AI

Guia passo-a-passo para depurar a extensão VsCode e o Sidecar .NET.

---

## 🔧 Pré-requisitos

| Ferramenta | Versão | Para quê |
|-----------|--------|----------|
| VsCode | 1.85+ | Editor + Extension Host |
| Node.js | 18+ | Compilar TypeScript |
| .NET 10 SDK | 10.0+ | Sidecar (standalone) |
| Git | — | Controle de versão |

---

## 🚀 Método 1: F5 — Extension Development Host (mais rápido)

### Passo 1: Abrir o projeto

```bash
code src/KrnlAI.VsCode
```

### Passo 2: Compilar TypeScript

```bash
cd src/KrnlAI.VsCode
npm install
npx tsc
```

> 💡 Dica: Use `npx tsc --watch` para recompilar automaticamente a cada alteração.

### Passo 3: Pressionar F5

Isso abre um **novo VsCode** (Extension Development Host) com a extensão carregada.

### Passo 4: Verificar se carregou

No novo VsCode:
- Barra lateral → ícone 🤖 (Krnl-AI)
- `Ctrl+Shift+P` → digite `Krnl-AI` → 8 comandos aparecem
- Status bar → `$(hubot)` com status de conexão

### Debug no VsCode Host

No VsCode **original** (janela do projeto):
- `Ctrl+Shift+Y` → Output → dropdown `Extension Host` → logs da extensão
- `Ctrl+\`` → Terminal integrado → logs do Sidecar
- Breakpoints em `src/extension.ts` funcionam normalmente

---

## 🐛 Método 2: Debug Completo (Extension + Sidecar + Kernel)

### Cenário: Extensão → Sidecar → KrnlAI.Core

```
VsCode Extension (F5)
    │ fetch("http://localhost:5001/agent/run")
    ▼
KrnlAI.Sidecar (Kestrel)
    │ AdversarialGuard.ValidateAsync()
    │ SimpleRiskScorer.ScoreRisk()
    ▼
KrnlAI.Core (in-process)
```

### Passo 1: Iniciar Sidecar em modo debug

```bash
# Terminal 1: Sidecar com debugger
cd src/KrnlAI.Sidecar
dotnet run
# Saída: "Now listening on http://localhost:5001"
```

Ou use o **VsCode Task Runner** para depurar o Sidecar ao mesmo tempo:

1. `Ctrl+Shift+P` → `Debug: Add Configuration` → escolha `.NET` → selecione `KrnlAI.Sidecar`
2. Isso cria `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Sidecar",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/KrnlAI.Sidecar/bin/Debug/net10.0/KrnlAI.Sidecar.dll",
            "args": ["--port", "5001"],
            "cwd": "${workspaceFolder}/src/KrnlAI.Sidecar",
            "console": "internalConsole"
        },
        {
            "name": "VsCode Extension",
            "type": "extensionHost",
            "request": "launch",
            "args": [
                "--extensionDevelopmentPath=${workspaceFolder}/src/KrnlAI.VsCode"
            ],
            "outFiles": ["${workspaceFolder}/src/KrnlAI.VsCode/out/**/*.js"]
        }
    ],
    "compounds": [
        {
            "name": "Extension + Sidecar",
            "configurations": ["VsCode Extension", ".NET Sidecar"]
        }
    ]
}
```

### Passo 3: Executar ambos simultaneamente

`Ctrl+Shift+D` → selecione `Extension + Sidecar` → F5

Agora você tem:
- ✅ Extensão VsCode com WebViews
- ✅ Sidecar .NET com KrnlAI.Core in-process
- ✅ Debug de ambos com breakpoints

---

## 🔬 Método 3: Debug Remoto (API)

### Cenário: Extensão → API remota (Gateway + Kernel)

Configure a extensão para usar o backend já rodando:

1. `Ctrl+Shift+P` → `Krnl-AI: Configurações`
2. Endpoint: `http://localhost:5000`
3. Modo: `API Remota`

Para ver as requisições HTTP:

```bash
# Monitorar chamadas da extensão
curl -s http://localhost:5000/health
# Se funcionar, a extensão também funciona
```

---

## 🧪 Testar WebViews Isoladamente

As WebViews são HTML puro e podem ser testadas no navegador:

```bash
# Servir HTML localmente
npx serve src/KrnlAI.VsCode/media
```

Abra `http://localhost:3000/chat.html` no navegador. As chamadas `acquireVsCodeApi()` falharão, mas o layout e o CSS podem ser validados.

---

## 🪵 Logs e Diagnóstico

### Logs da Extensão

| Onde | O quê |
|------|-------|
| `Ctrl+Shift+Y` → `Extension Host` | `console.log` do TypeScript |
| `Ctrl+\`` → Terminal | Logs do Sidecar (stdout/stderr) |
| `~/.config/Code/logs/` | Logs do VsCode |

### Logs do Sidecar

```bash
# Rodar com verbose
dotnet run --project src/KrnlAI.Sidecar -- --port 5001 --verbose
```

### Health Check

```bash
# Testar Sidecar
curl http://localhost:5001/health
# → {"status":"ok","ts":"2026-05-08T...","version":"KrnlAI.Sidecar/1.0.0"}

# Testar API remota
curl http://localhost:5000/health
# → {"ok":true}
```

---

## ❌ Problemas Comuns

| Problema | Causa | Solução |
|----------|-------|---------|
| `cannot find module 'vscode'` | `@types/vscode` não instalado | `npm install` |
| WebView em branco | `out/` desatualizado | `npx tsc` |
| `connect ECONNREFUSED :5001` | Sidecar não iniciado | `Krnl-AI: Iniciar Sidecar` |
| `fetch is not defined` | Node.js 18+ necessário | `node --version` |
| Sidecar trava | Porta ocupada | Mude a porta: `--port 5002` |
| TreeView vazio | `package.json` views incorreto | Verifique `contributes.views` |
| WebView não responde | `retainContextWhenHidden` ausente | Adicione ao criar o painel |

---

## 💻 Estrutura de Debug Completa

```
src/KrnlAI.VsCode/
├── src/
│   ├── extension.ts          ← Breakpoints aqui
│   ├── api/client.ts         ← Breakpoints nas chamadas HTTP
│   └── panels/
│       ├── chatPanel.ts      ← Breakpoints nos handlers
│       ├── dashboardPanel.ts
│       ├── policiesPanel.ts
│       ├── episodesPanel.ts
│       ├── memoryPanel.ts
│       └── settingsPanel.ts
├── out/                      ← JS compilado (editar aqui se quiser hotfix)
└── media/                    ← HTML/CSS (recarregar WebView com F5 nela)

src/KrnlAI.Sidecar/
└── Program.cs                ← Breakpoints nos endpoints /agent/run, /health
```

---

## 📝 Configuração VsCode (settings.json)

```json
{
    "krnlai.endpoint": "http://localhost:5000",
    "krnlai.standalone": true,
    "krnlai.sidecarPort": 5001,
    "debug.javascript.autoAttachFilter": "always"
}
```

---

## 🧠 Sistema de Debug Interno

### OperationTracker

Todas as operações internas da extensão são rastreadas automaticamente via `OperationTracker`:

```
TerminalManager.runCommand()
GitManager.commit()
DebugManager.launch()
DebugManager.stop()
DebugManager.setBreakpoint()
DebugManager.stepOver()
DebugManager.stepInto()
DebugManager.continue()
```

Para ver o trace em tempo real:

```bash
# Comando via Command Palette
Ctrl+Shift+P → "Krnl-AI: Debug Trace Panel"

# Ou atalho
Ctrl+Shift+'  (abre o DebugPanel com auto-refresh 2s)
```

O painel mostra:

```
### Debug Trace

✅ **debug.launch** — Completed (1.2s)
✅ **debug.stop** — Completed (0.3s)
❌ **debug.breakpoint.set** — Failed (0.1s)
  Args: `src/main.ts:42`
  Error: `File not found`
```

### DebugManager (vscode.debug API)

| Comando | Atalho | Ação |
|---------|--------|------|
| `Krnl-AI: Debug Launch` | `Ctrl+Shift+D` | `vscode.debug.startDebugging()` |
| `Krnl-AI: Debug Stop` | `Shift+F5` | `vscode.debug.stopDebugging()` |
| `Krnl-AI: Debug Step Over` | `F10` | `workbench.action.debug.stepOver` |
| `Krnl-AI: Debug Step Into` | `F11` | `workbench.action.debug.stepInto` |
| `Krnl-AI: Debug Continue` | `F5` | `workbench.action.debug.continue` |
| `Krnl-AI: Debug Breakpoint` | — | Prompt para `file:line` |
| `Krnl-AI: Debug Build` | — | `dotnet build` no terminal |
| `Krnl-AI: Debug Trace` | — | Webview com trace internos |

### Toolcall Debug (LLM)

O LLM pode invocar debug tools diretamente através do `DebugAgentTool` no LLMGateway:

```json
// Exemplo: LLM chama debug.run("MyApp")
{
    "tool": "debug.run",
    "input": { "project": "MyApp" }
}

// Mapeamento:
// debug.run         → /debug-run <project>
// debug.stop        → /debug-stop
// debug.breakpoint  → /debug-bp <file>:<line>
// debug.step_over   → /debug-step-over
// debug.step_into   → /debug-step-into
// debug.continue    → /debug-continue
// debug.build       → /debug-build
// debug.trace       → /debug [limit]
```

O fluxo completo:

```
LLM → debug.run("MyApp")
  → DebugAgentTool (LLMGateway.Core)
    → POST /commands/handle { command: "/debug-run MyApp" }
      → Kernel API → SlashCommandRouter
        → DebugManager.launch("MyApp")
          → vscode.debug.startDebugging()
```
