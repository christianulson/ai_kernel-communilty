# Exemplos

O repositorio Community inclui projetos de exemplo para demonstrar padroes comuns e casos de uso.

## Hello Agent

O fluxo de trabalho Krnl-AI mais simples possivel:

```bash
dotnet tool install -g KrnlAI.Cli
krnlai chat --local
```

Tente esta conversa:

```
> Lembre-se de que este projeto usa memoria local-first.
> O que voce lembra sobre este projeto?
```

O primeiro prompt escreve memoria atraves do kernel embutido. O segundo prompt a recupera atraves do runtime local.

Fonte: `samples/hello-agent/`

## Ferramenta Personalizada

Demonstra o padrao para criar ferramentas comunitarias personalizadas:

```csharp
public sealed record TodoInput(string Title);

public sealed class TodoTool
{
    public Task<string> ExecuteAsync(TodoInput input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Title);
        return Task.FromResult($"Tarefa criada: {input.Title}");
    }
}
```

### Principios de Design de Ferramentas

1. Defina o esquema de entrada da ferramenta
2. Valide a requisicao antes da execucao
3. Execute verificacoes de seguranca para acoes com efeitos colaterais
4. Retorne saida estruturada para o agente

Fonte: `samples/custom-tool/`

## Exemplos Python

O SDK Python inclui varios scripts de exemplo:

### Agente Basico (`examples/01-basic-agent/`)

```python
from krnlai import CognitiveAgent

agent = CognitiveAgent()
result = await agent.run("Ola, mundo!")
print(result.output)
```

### Suporte ao Cliente (`examples/02-customer-support/`)

Um agente de suporte ao cliente de varias voltas com persistencia de memoria.

### Assistente de Pesquisa (`examples/03-research-assistant/`)

Um agente que pesquisa memoria semantica e fornece resumos de pesquisa.

### Multi-Agente (`examples/04-multi-agent/`)

Dois agentes se comunicando atraves de memoria compartilhada.

### Modo Enterprise (`examples/05-enterprise-mode/`)

Conectando o SDK Python ao backend empresarial C#.

## Projetos Template

Inicialize templates reutilizaveis via CLI:

```bash
krnlai new agent my-agent       # Agente cognitivo basico
krnlai new tool my-tool          # Ferramenta personalizada
krnlai new policy my-policy      # Template de politica
krnlai new cognitive-cycle       # Ciclo cognitivo personalizado
```
