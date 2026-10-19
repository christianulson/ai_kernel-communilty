# Publicando a extensão novo-modelo no Visual Studio Marketplace

## Pacote

O build de `src/KrnlAI.VisualStudio.Extensibility` gera `KrnlAI.VisualStudio.Extensibility.vsix`
(~4.7 MB) no output `bin/Release/net8.0/`. Ele contém:

- `extension.vsixmanifest` com `ExtensionType="VisualStudio.Extensibility"` e target `[17.14,)`
- `.vsextension/extension.json` (entry point, commands, tool windows, provider de CodeLens)

Este `.vsix` é o pacote publicável (mesmo formato VSIX, com o novo modelo dentro).

## Fluxo

1. O workflow `vsix-extensibility-publish.yml` (tag `vsix-v*`) builda e publica o `.vsix` como artifact.
2. Publicação manual no portal: https://marketplace.visualstudio.com/manage
   - Conta **publisher** "Krnl-AI" (ou criar/aderir como publisher).
   - "New extension" → upload do `.vsix` → preencher release notes e categorias.
3. A extensão aparece em "Extensions" para VS 2022 17.14+ e VS 18.

## Validação local do pacote

```powershell
# Conteúdo esperado (varredura):
unzip -l src/KrnlAI.VisualStudio.Extensibility/bin/Release/net8.0/KrnlAI.VisualStudio.Extensibility.vsix
# deve listar extension.vsixmanifest, extension.vsextension/, as DLLs do novo modelo
```

## Notas

- O VS Marketplace não oferece CLI oficial de publicação (diferente do VS Code); o upload é pelo portal.
- Ainda não há servidor de atualização configurado — o updater do Tauri/VSIX reporta "not configured" (ver `BL-20260911-009`).
- Extensão herda o ID `KrnlAI.VisualStudio.a6b3f8e1-...` do legado (compatibilidade de marketplace).