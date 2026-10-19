# KrnlAI.VisualStudio — Migração para o novo SDK de Extensibility

> Status: migração núcleo concluída (esqueleto, CodeLens, commands, tool windows, serviços).
> SDK alvo: `Microsoft.VisualStudio.Extensibility.Sdk 17.14.40608` (roda no VS 2022 17.14+ e no VS 18 — homologação).

## Estrutura

| Projeto | Papel |
|---|---|
| `src/KrnlAI.VisualStudio.Extensibility` | Extensão novo-modelo (entry point, commands, tool windows RemoteUI, editor parts) |
| `src/KrnlAI.VisualStudio.Extensibility.Core` | Lógica pura testável (sem VS SDK): policies, models, serviços |
| `src/KrnlAI.VisualStudio` (legado) | VSIX clássico net472 — mantido até a migração completar (item 021); arquivos com APIs removidas permanecem com `#if AUTOCODE_ENABLE_CODELENS` |

## Perdas registradas (portar quando houver API)

| Feature | Status |
|---|---|
| Code Actions (lightbulb) | `ISuggestedActionSourceProvider` removido do VS 2022; substituto `ISuggestedActionsSource` existe mas a porta é item em aberto |
| CodeLens | Portado (API preview no 17.14 — `VSEXTPREVIEW_CODELENS` opt-in; estável no VS18) |
| Hover | `IHoverDisplay` removido — sem API equivalente no 17.14 |
| Completion (MEF clássico) | Sem API de completion no novo modelo 17.14 |
| Error List (AnalyzeErrorCommand) | Sem API de Error List no novo modelo 17.14 |
| InfoBar / Output window | Sem API equivalente no novo modelo |
| Message box de confirmação (SendSelectionToChat) | Substituído por log estruturado |
| Terminal integrado, integração debugger, DTE | Sem API equivalente (GitService, TerminalService, ApplyEditService, DebugState, SolutionContextService, VsDebugService) |
| VS settings store (WritableSettingsStore) | Substituído por `ISettingsStore` (in-memory; persistência real futura) |
| `IVsOperationTracker` (debug) | Substituído por `ILogger` |

## Itens futuros (BACKLOG.md)

- Portar Code Actions para `ISuggestedActionsSource` (17.14+)
- Portar Completion/Hover quando o SDK 18.x publicar as APIs
- Persistência real para `ISettingsStore` (JSON)
- Remover o projeto legado `KrnlAI.VisualStudio` após a migração completa
- Integrar `KernelClientService` ao chat (respostas do kernel na tool window)