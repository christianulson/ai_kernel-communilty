# Publicação nas lojas — VS Code e Browser Extension

## VS Code (`src/KrnlAI.VsCode`)

1. Secrets no repositório:
   - `VSCE_TOKEN` — token de publicação do VS Code Marketplace (Visual Studio → manage → Publishers → <publisher> → Personal Access Token)
   - `OVSX_TOKEN` — token do Open VSX (open-vsx.org, para distribuição aberta)
2. Triggers:
   - `vsix-publish.yml` — publica em tag `vscode-v*`
   - `release.yml` — publica junto com o release (usa `VSCE_TOKEN`/`OVSX_TOKEN`)
3. Comandos manuais (para validar localmente):
   ```bash
   cd src/KrnlAI.VsCode
   npm ci
   npm run build:bundle
   npx @vscode/vsce package --no-dependencies   # gera o .vsix
   npx @vscode/vsce publish                      # publica
   npx ovsx publish <arquivo>.vsix              # publica no Open VSX
   ```
4. O `publisher` no `package.json` deve existir no Marketplace (ex.: `krnlai`).

## Browser Extension (`src/KrnlAI.BrowserExtension`)

1. Build: `npm ci && npm run build` (gera `dist/`).
2. Lojas:
   - **Chrome Web Store**: upload do zip de `dist/` em https://chrome.google.com/webstore/devconsole (conta de desenvolvedor) — upload manual, sem CLI oficial.
   - **Firefox Add-ons**: https://addons.mozilla.org — upload manual; o MV3 é compatível.
   - **Edge Add-ons**: https://partner.microsoft.com/dashboard/microsoftedge — upload manual.
3. O workflow de CI já valida `npm ci && npm run lint && npm run build` (job `browser-extension`).
   A publicação nas lojas é manual (cada loja exige conta + revisão); sem secret automatizável por CLI.

## Notas

- Nunca commitar `dist/` nem credenciais de loja.
- Cada loja tem seu processo de revisão; planejar os uploads com antecedência de revisão.