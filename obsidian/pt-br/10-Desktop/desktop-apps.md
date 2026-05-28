# Aplicativos Desktop

O Krnl-AI Community agora inclui duas superfícies desktop: um aplicativo Windows em WPF e um aplicativo multiplataforma em Tauri. Ambos focam em fluxos local-first, estado de autenticação, controles de privacidade e navegação orientada a desenvolvedor.

## App Desktop WPF (Windows)

Um aplicativo desktop Windows completo construído com WPF (.NET 10).

### Recursos

- Interface de chat interativa
- Navegador e busca de memória
- Visualizador de memória episódica
- Gerenciamento de políticas
- Registro de modelos
- Gerenciamento de sessões
- Gerenciamento de arquivo/documentos
- Ferramentas de benchmark
- Visualização de grafo causal
- Dashboard com métricas
- Estado de auth, API keys e controles de privacidade
- Chamadas peer-to-peer via sinalização WebRTC
- Temas claro e escuro
- Suporte a vários idiomas (en, pt-BR)

### Execução

```bash
cd src/KrnlAI.Desktop.App
dotnet run
```

### Arquitetura

```
KrnlAI.Desktop.App/           → Camada de UI WPF (Views, ViewModels, Controls)
KrnlAI.Desktop.Core/          → Serviços e modelos compartilhados
KrnlAI.Desktop.Infrastructure/ → HTTP client, auth, settings
KrnlAI.Desktop.Tauri/         → Superfície UI multiplataforma
```

### Componentes Principais

| Componente | Descrição |
|-----------|-----------|
| `ChatControl` | Chat interativo com o agente |
| `MemoryControl` | Navegador de memória semântica e episódica |
| `PoliciesControl` | Visualizador e editor de políticas |
| `ModelRegistryControl` | Configuração de modelos LLM |
| `SessionsControl` | Gerenciamento de sessões |
| `DashboardControl` | Visão geral de métricas e performance |
| `ArchiveControl` | Memórias arquivadas |
| `CausalGraphControl` | Visualização de relações causais |
| `ApiKeysControl` | API keys self-service |
| `PrivacyControl` | Fluxos de consentimento, exportação e exclusão |
| `VideoCallViewModel` | Chamadas peer-to-peer via WebRTC |

## App Desktop Tauri (Multiplataforma)

Um aplicativo desktop multiplataforma construído com Tauri (backend Rust + frontend React/TypeScript).

### Recursos

- Interface de chat com comunicação com o sidecar
- Dashboard com status em tempo real
- Gerenciamento de configurações
- Persistência de estado de auth em localStorage
- Páginas de API keys e privacidade
- Ícone de bandeja com ações rápidas
- Notificações para eventos do agente
- Configurações compartilhadas para sessões P2P/WebRTC locais

### Execução (Desenvolvimento)

```bash
cd src/KrnlAI.Desktop.Tauri
npm install
npm run tauri dev
```

### Arquitetura

```
KrnlAI.Desktop.Tauri/
├── src/                    → Frontend React/TypeScript
│   ├── App.tsx             → Componente principal
│   ├── SidecarClient.ts    → Cliente da API do sidecar
│   ├── TauriBridge.ts      → Bridge nativa Tauri
│   ├── components/         → Componentes de UI
│   └── pages/              → Páginas do aplicativo
├── src-tauri/              → Backend nativo Rust
│   ├── src/main.rs         → Ponto de entrada
│   ├── src/commands.rs     → Comandos IPC do Tauri
│   ├── src/sidecar.rs      → Gerenciamento do processo sidecar
│   ├── src/tray.rs         → Integração com bandeja do sistema
│   └── src/notifications.rs → Notificações nativas
└── package.json
```

### Build

```bash
npm run tauri build
```

O binário final fica em `src-tauri/target/release/`.

## P2P / WebRTC

O cliente desktop suporta sessões de vídeo peer-to-peer por meio de um endpoint WebSocket de sinalização em `/signaling/webrtc`.

### Fluxo

- O `WebRtcService` do WPF inicializa um peer local e conecta no endpoint de sinalização.
- A UI pode criar uma offer ou entrar em um peer por id.
- Áudio/vídeo trafegam pelo canal de sinalização enquanto a mídia em si permanece peer-to-peer.
- A configuração STUN/TURN vem da superfície de configurações do desktop.

### Superfície atual

- `VideoCallViewModel` controla transições de estado da chamada
- `WebRtcService` gerencia as mensagens de sinalização
- `SettingsViewModel` expõe os campos de STUN/TURN
