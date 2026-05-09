# Debug da Extensão VsCode — AI Kernel

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
code src/AIKernel.VsCode
```

### Passo 2: Compilar TypeScript

```bash
cd src/AIKernel.VsCode
npm install
npx tsc
```

> 💡 Dica: Use `npx tsc --watch` para recompilar automaticamente a cada alteração.

### Passo 3: Pressionar F5

Isso abre um **novo VsCode** (Extension Development Host) com a extensão carregada.

### Passo 4: Verificar se carregou

No novo VsCode:
- Barra lateral → ícone 🤖 (AI Kernel)
- `Ctrl+Shift+P` → digite `AI Kernel` → 8 comandos aparecem
- Status bar → `$(hubot)` com status de conexão

### Debug no VsCode Host

No VsCode **original** (janela do projeto):
- `Ctrl+Shift+Y` → Output → dropdown `Extension Host` → logs da extensão
- `Ctrl+\`` → Terminal integrado → logs do Sidecar
- Breakpoints em `src/extension.ts` funcionam normalmente

---

## 🐛 Método 2: Debug Completo (Extension + Sidecar + Kernel)

### Cenário: Extensão → Sidecar → Kernel.Core

```
VsCode Extension (F5)
    │ fetch("http://localhost:5001/agent/run")
    ▼
AIKernel.Sidecar (Kestrel)
    │ AdversarialGuard.ValidateAsync()
    │ SimpleRiskScorer.ScoreRisk()
    ▼
Kernel.Core (in-process)
```

### Passo 1: Iniciar Sidecar em modo debug

```bash
# Terminal 1: Sidecar com debugger
cd src/AIKernel.Sidecar
dotnet run
# Saída: "Now listening on http://localhost:5001"
```

Ou use o **VsCode Task Runner** para depurar o Sidecar ao mesmo tempo:

1. `Ctrl+Shift+P` → `Debug: Add Configuration` → escolha `.NET` → selecione `AIKernel.Sidecar`
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
            "program": "${workspaceFolder}/src/AIKernel.Sidecar/bin/Debug/net10.0/AIKernel.Sidecar.dll",
            "args": ["--port", "5001"],
            "cwd": "${workspaceFolder}/src/AIKernel.Sidecar",
            "console": "internalConsole"
        },
        {
            "name": "VsCode Extension",
            "type": "extensionHost",
            "request": "launch",
            "args": [
                "--extensionDevelopmentPath=${workspaceFolder}/src/AIKernel.VsCode"
            ],
            "outFiles": ["${workspaceFolder}/src/AIKernel.VsCode/out/**/*.js"]
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
- ✅ Sidecar .NET com Kernel.Core in-process
- ✅ Debug de ambos com breakpoints

---

## 🔬 Método 3: Debug Remoto (API)

### Cenário: Extensão → API remota (Gateway + Kernel)

Configure a extensão para usar o backend já rodando:

1. `Ctrl+Shift+P` → `AI Kernel: Configurações`
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
npx serve src/AIKernel.VsCode/media
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
dotnet run --project src/AIKernel.Sidecar -- --port 5001 --verbose
```

### Health Check

```bash
# Testar Sidecar
curl http://localhost:5001/health
# → {"status":"ok","ts":"2026-05-08T...","version":"AIKernel.Sidecar/1.0.0"}

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
| `connect ECONNREFUSED :5001` | Sidecar não iniciado | `AI Kernel: Iniciar Sidecar` |
| `fetch is not defined` | Node.js 18+ necessário | `node --version` |
| Sidecar trava | Porta ocupada | Mude a porta: `--port 5002` |
| TreeView vazio | `package.json` views incorreto | Verifique `contributes.views` |
| WebView não responde | `retainContextWhenHidden` ausente | Adicione ao criar o painel |

---

## 💻 Estrutura de Debug Completa

```
src/AIKernel.VsCode/
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

src/AIKernel.Sidecar/
└── Program.cs                ← Breakpoints nos endpoints /agent/run, /health
```

---

## 📝 Configuração VsCode (settings.json)

```json
{
    "aikernel.endpoint": "http://localhost:5000",
    "aikernel.standalone": true,
    "aikernel.sidecarPort": 5001,
    "debug.javascript.autoAttachFilter": "always"
}
```
