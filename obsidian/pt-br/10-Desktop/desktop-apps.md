# Aplicativos Desktop

Krnl-AI Community inclui duas opcoes de aplicativo desktop: um aplicativo WPF completo para Windows e um aplicativo Tauri multiplataforma.

## App Desktop WPF (Windows)

Um aplicativo desktop Windows completo construido com WPF (.NET 10).

### Recursos

- Interface de chat interativa
- Navegador e busca de memoria
- Visualizador de memoria episodica
- Gerenciamento de politicas
- Registro de modelos
- Gerenciamento de sessoes
- Gerenciamento de arquivo/documentos
- Ferramentas de benchmark
- Visualizacao de grafo causal
- Dashboard com metricas
- Temas claro e escuro
- Suporte a varios idiomas (en, pt-BR)

### Executando

```bash
cd src/KrnlAI.Desktop.App
dotnet run
```

### Arquitetura

```
KrnlAI.Desktop.App/      → Camada de UI WPF (Views, ViewModels, Controls)
KrnlAI.Desktop.Core/     → Servicos e modelos compartilhados
KrnlAI.Desktop.Infrastructure/ → Cliente HTTP, autenticacao, configuracoes
```

### Componentes Principais

| Componente | Descricao |
|-----------|-------------|
| `ChatControl` | Chat interativo com o agente |
| `MemoryControl` | Navegador de memoria semantica e episodica |
| `PoliciesControl` | Visualizador e editor de politicas |
| `ModelRegistryControl` | Configuracao de modelo LLM |
| `SessionsControl` | Gerenciamento de sessoes |
| `DashboardControl` | Metricas e visao geral de performance |
| `ArchiveControl` | Memorias arquivadas |
| `CausalGraphControl` | Visualizacao de relacoes causais |

## App Desktop Tauri (Multiplataforma)

Um aplicativo desktop multiplataforma construido com Tauri (backend Rust + frontend React/TypeScript).

### Recursos

- Interface de chat com comunicacao com sidecar
- Dashboard com status em tempo real
- Gerenciamento de configuracoes
- Icone de bandeja com acoes rapidas
- Notificacoes para eventos do agente

### Executando (Desenvolvimento)

```bash
cd src/KrnlAI.Desktop.Tauri
npm install
npm run tauri dev
```

### Arquitetura

```
KrnlAI.Desktop.Tauri/
├── src/                    → Frontend React/TypeScript
│   ├── App.tsx             → Componente principal da aplicacao
│   ├── SidecarClient.ts   → Cliente de API para sidecar
│   ├── TauriBridge.ts     → Ponte de API nativa Tauri
│   ├── components/        → Componentes de UI
│   └── pages/             → Paginas da aplicacao
├── src-tauri/              → Backend nativo Rust
│   ├── src/main.rs         → Ponto de entrada da aplicacao
│   ├── src/commands.rs     → Comandos IPC do Tauri
│   ├── src/sidecar.rs      → Gerenciamento de processo sidecar
│   ├── src/tray.rs         → Integracao com bandeja do sistema
│   └── src/notifications.rs → Notificacoes nativas
└── package.json
```

### Compilando

```bash
npm run tauri build
```

O binario compilado estara em `src-tauri/target/release/`.
