# Publicando KrnlAI.Cli como dotnet tool (NuGet)

O `KrnlAI.Cli` já está configurado como tool (`PackAsTool=true`, `ToolCommandName=krnlai`) e o pacote é gerado com sucesso localmente (validação no BL-20260911-007).

## Fluxo

1. Criar uma tag `cli-v*` (ex.: `cli-v1.0.0`) e push — o workflow `cli-publish.yml` empacota e publica em nuget.org.
2. O secret `NUGET_API_KEY` deve existir no repositório (GitHub → Settings → Secrets).

## Obtendo a API key

1. Acesse https://www.nuget.org/account/apikeys (logado com a conta dona do id `KrnlAI.Cli`).
2. Crie uma chave com escopo `Push` para o pacote `KrnlAI.Cli`.
3. Adicione como secret `NUGET_API_KEY` no repositório do submódulo.

## Uso pelo usuário final

```
dotnet tool install -g krnlai
```

O binário expõe o comando `krnlai` (ex.: `krnlai --help`, `krnlai status`).

## Smoke local (sem chave)

O pack + instalação local são verificáveis com:

```powershell
dotnet pack src/KrnlAI.Cli -c Release -p:SignAssembly=false
dotnet tool install --tool-path <temp> KrnlAI.Cli --add-source .\src\KrnlAI.Cli\nupkg --version 1.0.0
<temp>\krnlai.exe --help
```
(Nota: `--add-source` exige config sem package source mapping — veja o teste do BL-20260911-007.)