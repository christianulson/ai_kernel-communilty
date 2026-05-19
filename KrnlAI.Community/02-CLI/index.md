# CLI Reference

The Krnl-AI CLI (`krnlai`) is the primary interface for interacting with the community runtime.

## Core Commands

### `krnlai chat --local`

Start an interactive TUI chat session with the local embedded kernel.

```bash
krnlai chat --local
krnlai chat --local --model llama3.1
```

### `krnlai init <name>`

Create a new agent project with scaffold files.

```bash
krnlai init my-agent
```

### `krnlai run <text>`

Run the agent once with the given input text.

```bash
krnlai run "analyze this dataset"
krnlai run --interactive
```

### `krnlai serve --local --port <port>`

Start the sidecar HTTP server for editor integrations and automation.

```bash
krnlai serve --local --port 5117
```

## Memory Commands

### `krnlai memory search <query>`

Search semantic memory for relevant documents.

```bash
krnlai memory search "project decision"
```

### `krnlai memory snapshot`

Create a snapshot of current memory state.

```bash
krnlai memory snapshot
```

### `krnlai memory metrics`

View memory usage statistics.

```bash
krnlai memory metrics
```

## Safety Commands

### `krnlai safety run`

Execute safety checks against the current configuration.

```bash
krnlai safety run
```

### `krnlai safety status`

Display the status of all safety layers.

```bash
krnlai safety status
```

### `krnlai security audit`

Run a full security audit of the safety system.

```bash
krnlai security audit
```

### `krnlai security benchmark <count>`

Performance benchmark the safety system (default: 1000 iterations).

```bash
krnlai security benchmark 5000
```

### `krnlai security report <file>`

Generate an HTML security report.

```bash
krnlai security report report.html
```

## Skill Commands

### `krnlai skill list`

List installed skills.

```bash
krnlai skill list
```

### `krnlai skill export <name> <file>`

Export a skill for sharing.

```bash
krnlai skill export my-skill skill.json
```

### `krnlai skill import <file>`

Import a skill from a file.

```bash
krnlai skill import skill.json
```

## Policy Commands

### `krnlai policy list`

List learned policies.

```bash
krnlai policy list
```

### `krnlai policy show <id>`

Show details of a specific policy.

```bash
krnlai policy show policy-1
```

## Session Commands

### `krnlai session list`

List all sessions.

```bash
krnlai session list
```

### `krnlai session export <id> <file>`

Export a session for sharing or analysis.

```bash
krnlai session export session-1 session.json
```

## Utility Commands

### `krnlai config list`

Show all configuration values.

```bash
krnlai config list
```

### `krnlai config set <key> <value>`

Set a configuration value.

```bash
krnlai config set model llama3.1
```

### `krnlai status`

Show the current status of the Krnl-AI runtime.

```bash
krnlai status
```

### `krnlai health`

Check the health of the runtime and configured providers.

```bash
krnlai health
```

### `krnlai upgrade`

Upgrade the CLI to the latest version.

```bash
krnlai upgrade
```

## Template Commands

### `krnlai templates list`

List available project templates.

```bash
krnlai templates list
```

### `krnlai new <template> <name>`

Create a new project from a template.

```bash
krnlai new agent my-agent
krnlai new tool my-tool
krnlai new policy my-policy
```
