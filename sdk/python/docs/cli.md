# CLI Reference

## Commands

### `krnlai init <name>`

Creates a new agent project with template files.

### `krnlai run [text]`

Runs the agent once with the given text.

Options:
- `--interactive`, `-i`: Start interactive chat mode

### `krnlai debug [text]`

Runs the agent with detailed step-by-step debug output.

### `krnlai security <command>`

Security tools:

- `audit`: Run safety system audit
- `benchmark [count]`: Performance benchmark (default: 1000)
- `report [file]`: Generate HTML security report

### `krnlai deploy <target> [name]`

Generate deployment files:

- `docker`: Creates Dockerfile + docker-compose.yml
- `kubernetes`: Creates K8s deployment manifest
