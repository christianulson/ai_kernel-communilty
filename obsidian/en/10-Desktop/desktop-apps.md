# Desktop Applications

Krnl-AI Community now includes two desktop application surfaces: a Windows WPF application and a cross-platform Tauri application. Both are focused on local-first workflows, auth state, privacy controls, and developer-friendly navigation.

## WPF Desktop App (Windows)

A full-featured Windows desktop application built with WPF (.NET 10).

### Features

- Interactive chat interface
- Memory browser and search
- Episodic memory viewer
- Policy management
- Model registry
- Session management
- Archive/document management
- Benchmarking tools
- Causal graph visualization
- Dashboard with metrics
- Auth state, API keys, and privacy controls
- Peer-to-peer video calling via WebRTC signaling
- Dark and light themes
- Multi-language support (en, pt-BR)

### Running

```bash
cd src/KrnlAI.Desktop.App
dotnet run
```

### Architecture

```
KrnlAI.Desktop.App/           → WPF UI layer (Views, ViewModels, Controls)
KrnlAI.Desktop.Core/          → Shared services and models
KrnlAI.Desktop.Infrastructure/ → HTTP client, auth, settings
KrnlAI.Desktop.Tauri/         → Cross-platform UI surface
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
| `ApiKeysControl` | Self-service API keys |
| `PrivacyControl` | Consent, export, and deletion flows |
| `VideoCallViewModel` | WebRTC peer-to-peer calling |

## Tauri Desktop App (Cross-Platform)

A cross-platform desktop application built with Tauri (Rust backend + React/TypeScript frontend).

### Features

- Chat interface with sidecar communication
- Dashboard with real-time status
- Settings management
- Auth state persistence in localStorage
- API keys and privacy pages
- Tray icon with quick actions
- Notifications for agent events
- Shared P2P/WebRTC signaling settings for local peer sessions

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
│   ├── SidecarClient.ts    → API client for sidecar
│   ├── TauriBridge.ts      → Native Tauri API bridge
│   ├── components/         → UI components
│   └── pages/              → Application pages
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

## P2P / WebRTC

The desktop client supports peer-to-peer video sessions through a WebSocket signaling endpoint at `/signaling/webrtc`.

### Flow

- The WPF `WebRtcService` initializes a local peer id and connects to the signaling endpoint.
- The UI can create an offer or join a peer by id.
- Audio/video frames are exchanged through the signaling layer while the media path stays peer-to-peer.
- STUN/TURN configuration is provided from the desktop settings surface.

### Current surface

- `VideoCallViewModel` handles call state transitions
- `WebRtcService` manages signaling messages
- `SettingsViewModel` exposes STUN/TURN configuration fields
