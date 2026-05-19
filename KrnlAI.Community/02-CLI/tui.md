# TUI (Terminal UI)

The CLI includes a terminal-based UI activated with `krnlai chat --local`.

## Features

- Split-panel layout with chat history and status
- Real-time session management
- Provider and model switching
- Built-in memory search
- Safety check status display

## Key Bindings

| Key | Action |
|-----|--------|
| `Ctrl+C` | Exit |
| `Ctrl+L` | Clear screen |
| `Ctrl+S` | Save session |
| `Tab` | Focus next panel |
| `Up/Down` | Navigate history |
| `Ctrl+R` | Search memory |
| `Esc` | Cancel / Close panel |

## Status Panel

The status panel displays:
- Connected provider and model
- Memory usage statistics
- Active safety layers
- Current session ID
- Error and warning indicators
