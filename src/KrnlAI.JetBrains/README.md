# Krnl-AI — JetBrains Extension

AI-powered coding assistant for IntelliJ IDEA, PyCharm, WebStorm, GoLand, and Rider.

## Features

- **Chat** — Interactive AI chat inside your IDE using JCEF WebView
- **Cognitive Dashboard** — Real-time system health, active modules, and emotional state
- **Slash Commands** — `/explain`, `/fix`, `/test` with keyboard shortcuts
- **Inline Completion** — AI-powered code suggestions as you type
- **Memory** — Search and ingest project context into Krnl-AI memory
- **Status Bar Widget** — Connection status and mood indicator

## Prerequisites

- IntelliJ IDEA 2024.3+ (or any JetBrains IDE based on 243.*)
- Java 17+
- [KrnlAI Sidecar](https://github.com/krnl-ai/sidecar) running on `http://127.0.0.1:5001`

## Build

```bash
cd Community/src/KrnlAI.JetBrains
./gradlew buildPlugin
```

## Install

1. Build the plugin: `./gradlew buildPlugin`
2. In your IDE: **Settings** → **Plugins** → **⚙️** → **Install Plugin from Disk...**
3. Select `build/distributions/KrnlAI.JetBrains-1.0.0.zip`
4. Restart IDE

## Configure

1. **Settings** → **Tools** → **Krnl-AI**
2. Set the **Sidecar URL** (default: `http://127.0.0.1:5001`)
3. Optionally set API Key and Auth Token
4. Enable/disable features as needed

## Usage

### Chat
- Open **Krnl-AI Chat** tool window (right side)
- Type your question in the input field
- Press **Send** or **Enter**

### Dashboard
- Open **Krnl-AI Dashboard** tool window (right side)
- Auto-refreshes every 5 seconds

### Slash Commands
| Command | Shortcut | Description |
|---------|----------|-------------|
| `/explain` | `Alt+Shift+E` | Explain selected code |
| `/fix` | `Alt+Shift+F` | Suggest fixes for diagnostics |
| `/test` | `Alt+Shift+T` | Generate unit tests |

### Status Bar
- Click the Krnl-AI status indicator to open the Dashboard
- Shows connection status: ⚡ connected, ⛔ disconnected
- Shows mood emoji based on emotional state

## Project Structure

```
KrnlAI.JetBrains/
├── build.gradle.kts              # Gradle build with intellij-plugin
├── settings.gradle.kts
├── gradle.properties
├── src/main/
│   ├── kotlin/com/krnlai/jetbrains/
│   │   ├── KrnlAIPlugin.kt           # Entry point / lifecycle
│   │   ├── client/
│   │   │   ├── KrnlAIClient.kt       # HTTP client for Sidecar
│   │   │   └── EditorContextProvider.kt  # Editor context extraction
│   │   ├── ui/
│   │   │   ├── ChatToolWindow.kt     # Chat panel (JCEF)
│   │   │   ├── DashboardToolWindow.kt # Dashboard panel (JCEF)
│   │   │   └── StatusBarWidget.kt    # Status bar indicator
│   │   ├── actions/
│   │   │   ├── ExplainAction.kt      # /explain command
│   │   │   ├── FixAction.kt          # /fix command
│   │   │   └── TestAction.kt         # /test command
│   │   ├── settings/
│   │   │   └── KrnlAISettings.kt     # Persistent settings + config UI
│   │   └── completion/
│   │       └── KrnlAICompletionProvider.kt  # Inline completion
│   ├── resources/
│   │   ├── META-INF/plugin.xml
│   │   └── icons/pluginIcon.svg
└── README.md
```

## Development

Open the `KrnlAI.JetBrains` directory as a project in IntelliJ IDEA and run `./gradlew runIde` to launch a test IDE instance with the plugin loaded.

```bash
./gradlew runIde
```
