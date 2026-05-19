# Desktop Applications

Krnl-AI Community includes two desktop application options: a full-featured WPF application for Windows and a cross-platform Tauri application.

## WPF Desktop App (Windows)

A full-featured Windows desktop application built with WPF (.NET 10).

### Features

- Interactive chat interface
- Memory browser and search
- Episodic memory viewer
- Policy management
- Model registry
- Sessions management
- Archive/document management
- Benchmarking tools
- Causal graph visualization
- Dashboard with metrics
- Dark and light themes
- Multi-language support (en, pt-BR)

### Running

```bash
cd src/KrnlAI.Desktop.App
dotnet run
```

### Architecture

```
KrnlAI.Desktop.App/      → WPF UI layer (Views, ViewModels, Controls)
KrnlAI.Desktop.Core/     → Shared services and models
KrnlAI.Desktop.Infrastructure/ → HTTP client, auth, settings
```

### Key Components

| Component | Description |
|-----------|-------------|
| `ChatControl` | Interactive chat with the agent |
| `MemoryControl` | Semantic and episodic memory browser |
| `PoliciesControl` | Policy viewer and editor |
| `ModelRegistryControl` | LLM model configuration |
| `SessionsControl` | Session management |
| `DashboardControl` | Metrics and performance overview |
| `ArchiveControl` | Archived memories |
| `CausalGraphControl` | Causal relationship visualization |

## Tauri Desktop App (Cross-Platform)

A cross-platform desktop application built with Tauri (Rust backend + React/TypeScript frontend).

### Features

- Chat interface with sidecar communication
- Dashboard with real-time status
- Settings management
- Tray icon with quick actions
- Notifications for agent events

### Running (Development)

```bash
cd src/KrnlAI.Desktop.Tauri
npm install
npm run tauri dev
```

### Architecture

```
KrnlAI.Desktop.Tauri/
├── src/                    → React/TypeScript frontend
│   ├── App.tsx             → Main application component
│   ├── SidecarClient.ts   → API client for sidecar
│   ├── TauriBridge.ts     → Native Tauri API bridge
│   ├── components/        → UI components
│   └── pages/             → Application pages
├── src-tauri/              → Rust native backend
│   ├── src/main.rs         → Application entry point
│   ├── src/commands.rs     → Tauri IPC commands
│   ├── src/sidecar.rs      → Sidecar process management
│   ├── src/tray.rs         → System tray integration
│   └── src/notifications.rs → Native notifications
└── package.json
```

### Building

```bash
npm run tauri build
```

The built binary will be in `src-tauri/target/release/`.
