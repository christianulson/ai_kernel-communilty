from __future__ import annotations

import os

from rich.console import Console

console = Console()


async def cmd_deploy(args: list) -> None:
    if not args:
        console.print("[yellow]Usage: krnlai deploy <docker|kubernetes> [name][/yellow]")
        return

    target = args[0]
    name = args[1] if len(args) > 1 else "krnlai-agent"

    if target == "docker":
        _generate_docker(name)
    elif target == "kubernetes":
        _generate_kubernetes(name)
    else:
        console.print(f"[red]Unknown deploy target: {target}[/red]")


def _generate_docker(name: str) -> None:
    dockerfile = """FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["krnlai", "run", "--interactive"]
"""

    compose = f"""version: '3.8'
services:
  {name}:
    build: .
    environment:
      - OPENAI_API_KEY=${{OPENAI_API_KEY:-}}
      - ANTHROPIC_API_KEY=${{ANTHROPIC_API_KEY:-}}
    volumes:
      - ./data:/app/data
"""

    with open("Dockerfile", "w") as f:
        f.write(dockerfile)
    with open("docker-compose.yml", "w") as f:
        f.write(compose)

    console.print(f"[green]Docker files generated for '{name}'[/green]")
    console.print("  Dockerfile")
    console.print("  docker-compose.yml")
    console.print("\nNext steps:")
    console.print("  docker-compose up")


def _generate_kubernetes(name: str) -> None:
    deployment = f"""apiVersion: apps/v1
kind: Deployment
metadata:
  name: {name}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: {name}
  template:
    metadata:
      labels:
        app: {name}
    spec:
      containers:
      - name: {name}
        image: {name}:latest
        env:
        - name: OPENAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: krnlai-secrets
              key: openai-api-key
"""

    os.makedirs("k8s", exist_ok=True)
    with open("k8s/deployment.yaml", "w") as f:
        f.write(deployment)
    console.print("[green]Kubernetes manifest generated: k8s/deployment.yaml[/green]")
    console.print("  kubectl apply -f k8s/")
