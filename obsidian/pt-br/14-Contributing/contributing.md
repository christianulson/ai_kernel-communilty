# Contribuindo

## Principios de Desenvolvimento

1. **TDD (Desenvolvimento Orientado a Testes)** — Escreva testes antes do codigo de producao
2. **Seguranca Primeiro** — Todas as contribuicoes devem respeitar o modelo de seguranca
3. **Testes Deterministicos** — Testes devem ser offline, rapidos e deterministicos
4. **Local-First** — Sem dependencias de servicos externos no modo comunitario

## Requisitos de Teste

- Framework: **xUnit** (C#) / **pytest** (Python)
- Todos os testes devem executar offline
- Sem chaves de API ou segredos codificados
- Cada teste individual < 5 segundos
- Mesma entrada → mesmo resultado

## Compilacao e Teste

### Projetos .NET

```bash
# Restaurar
dotnet restore krnlai.slnx

# Compilar
dotnet build krnlai.slnx

# Testar todos
dotnet test krnlai.slnx

# Testar projeto especifico
dotnet test tests/KrnlAI.Tests/KrnlAI.Tests.csproj

# Testar com filtro
dotnet test --filter "FullyQualifiedName~SafetyCommandTests"
```

### SDK Python

```bash
cd Community/sdk/python
pip install -e .
pytest
```

### Web/Tauri

- `Community/src/KrnlAI.Desktop.Tauri` contem a superficie desktop multiplataforma
- O trabalho de desktop agora inclui estado de auth, API keys, controles de privacidade e documentacao de P2P/WebRTC

```bash
# Veja `Community/src/KrnlAI.Desktop.Tauri/` para o frontend React
```

## Checklist de Pull Request

- [ ] Compilacao sem erros ou avisos
- [ ] Todos os testes passando
- [ ] Novo codigo tem testes correspondentes
- [ ] Testes escritos ANTES do codigo (TDD)
- [ ] Nenhum teste dependente de rede/servicos externos
- [ ] Documentacao atualizada se aplicavel
- [ ] Alteracoes focadas em uma unica preocupacao

## Boas Primeiras Issues

- Melhorias de documentacao
- Codigo de exemplo e padroes
- Pequenas adicoes de fluxo CLI
- Cobertura de testes para recursos existentes
- Exemplos de provedores para novos backends LLM
- Correcoes de bugs com reproducao simples

## Estilo de Codigo

- C#: Nullable habilitado, usings implicitos, versao de linguagem mais recente
- Python: Dicas de tipo, padroes async/await
- Siga as convencoes existentes no codigo base que voce esta modificando

## Obtendo Ajuda

- **GitHub Issues** — Relatorios de bug e solicitacoes de funcionalidades
- **GitHub Discussions** — Perguntas e respostas e showcase da comunidade
