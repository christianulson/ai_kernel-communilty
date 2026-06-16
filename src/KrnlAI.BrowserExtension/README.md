# Krnl-AI Browser Extension

Cognitive agent for your browser — chat with Krnl-AI, extract page context to memory, search memories, and more.

## Features

- **Chat Popup** (`Ctrl+Shift+K`): Quick chat with Krnl-AI
- **Side Panel**: Persistent chat across tabs with slash commands
- **Page Context**: Automatically attaches current page context to messages
- **Slash Commands**:
  - `/resume` — Summarize the current page
  - `/translate` — Translate page content
  - `/extract` — Save page to Krnl-AI memory
  - `/ask` — Ask a question about the page
- **Memory Browser**: Search semantic memory from the sidebar
- **Context Menus**: Right-click anywhere → "Explain this page" or "Extract to memory"
- **Connection Status**: Real-time health indicator

## Setup

### 1. Build

```bash
cd Community/src/KrnlAI.BrowserExtension
npm install
npm run build
```

### 2. Load in Browser

#### Chrome / Edge
1. Go to `chrome://extensions` / `edge://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked**
4. Select the `dist/` folder

#### Firefox
1. Go to `about:debugging#/runtime/this-firefox`
2. Click **Load Temporary Add-on**
3. Select the `dist/manifest.json`

### 3. Configure

1. Click the Krnl-AI icon in the toolbar
2. Click ⚙ to open settings
3. Set the API endpoint (default: `http://localhost:5100`)

## Requirements

- Krnl-AI Sidecar running on `localhost:5100` (or a remote API)
- Chrome 88+ / Edge 88+ / Firefox 109+

## Architecture

```
Popup / Sidebar UI (React)
    ↓ chrome.runtime.sendMessage
Background Service Worker
    ↓ fetch
Krnl-AI Sidecar (localhost:5100)
    ↓
Krnl-AI Cognitive Engine
```

## Development

```bash
npm run dev    # Watch mode with HMR for popup/sidebar
npm run build  # Production build
npm run lint   # TypeScript check
```
