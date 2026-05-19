# Editor Extensions

Krnl-AI integrates with popular code editors to bring cognitive agent capabilities directly into your development workflow.

## VS Code Extension

A TypeScript-based extension that adds Krnl-AI features to VS Code.

### Features

- **Chat Panel** — Interact with the agent directly in VS Code
- **Inline Completions** — AI-powered code suggestions
- **Code Actions** — Refactor, explain, and generate code
- **Coding Agent** — Autonomous agentic loop for complex tasks
- **Memory Integration** — Search and browse agent memory
- **Episode Viewer** — Browse execution history
- **Policy Viewer** — View and manage policies
- **Git Integration** — Auto-review, commit messages, PR descriptions
- **Session Teleport** — Persist and restore sessions

### Chat Commands

| Command | Description |
|---------|-------------|
| `Ctrl+Shift+P` → `KrnlAI: Chat` | Open chat panel |
| `Ctrl+Shift+P` → `KrnlAI: Inline` | Request inline completion |

### Chat Participants

The extension includes a `@krnlai` chat participant for the VS Code native chat interface:

```
@krnlai explain this function
@krnlai review my changes
@krnlai generate tests for this class
```

### Dashboard Panel

Provides real-time metrics and status of the cognitive runtime.

### Installation

```bash
# From VS Code Marketplace (when published)
ext install KrnlAI.VsCode

# From source
cd src/KrnlAI.VsCode
npm install
npm run compile
```

## Visual Studio Extension

A .NET-based extension for Visual Studio 2022.

### Features

- **Tool Window** — Dedicated Krnl-AI panel
- **Send Selection to Chat** — Send selected code to the agent
- **Analyze Error** — Get AI-powered error analysis
- **Chat History** — Persistent conversation history

### Tool Window Commands

| Command | Description |
|---------|-------------|
| `View → Other Windows → Krnl-AI` | Open the Krnl-AI tool window |
| Right-click → `Send to Krnl-AI` | Send selected code to chat |
| Right-click on error → `Analyze with Krnl-AI` | Analyze build error |

### Configuration

Settings available via `Tools → Options → Krnl-AI`:

- Sidecar endpoint URL
- Default provider
- Safety level
- Theme preference

### Installation

Build and install the VSIX from `src/KrnlAI.VisualStudio/`:

```bash
cd src/KrnlAI.VisualStudio
dotnet build
# Install bin/Debug/KrnlAI.VisualStudio.vsix
```
