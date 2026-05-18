# Getting Started with the CLI

The AI Kernel CLI is the fastest way to use the community runtime locally.

## Chat Locally

```bash
krnlai chat --local
```

Use `--model` to choose a configured model:

```bash
krnlai chat --local --model llama3.1
```

## Run the Sidecar

```bash
krnlai serve --local --port 5117
```

The sidecar is useful for editor integrations, local tools, and scripts.

## Common Commands

```bash
krnlai config list
krnlai memory search "project decision"
krnlai skill list
krnlai safety run
```

## Troubleshooting

If the CLI cannot reach a model, check the configured provider, endpoint, API key,
and model name. Local endpoints must be running before starting the CLI session.
