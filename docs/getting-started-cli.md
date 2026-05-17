# Getting Started with the CLI

The AI Kernel CLI is the fastest way to use the community runtime locally.

## Chat Locally

```bash
aikernel chat --local
```

Use `--model` to choose a configured model:

```bash
aikernel chat --local --model llama3.1
```

## Run the Sidecar

```bash
aikernel serve --local --port 5117
```

The sidecar is useful for editor integrations, local tools, and scripts.

## Common Commands

```bash
aikernel config list
aikernel memory search "project decision"
aikernel skill list
aikernel safety run
```

## Troubleshooting

If the CLI cannot reach a model, check the configured provider, endpoint, API key,
and model name. Local endpoints must be running before starting the CLI session.
