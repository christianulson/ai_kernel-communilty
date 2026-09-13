# Release Signing — Runbook

> Status: **not implemented** (this is the plan/reference for enabling it).
> Scope: Krnl-AI Desktop (WPF), Tauri Desktop, Sidecar executables.

## Windows — Authenticode

1. Obtain a code-signing certificate (e.g., DigiCert/GlobalSign EV or standard OV).
2. Export as `.pfx` (or use Azure Key Vault signing).
3. In CI, store the certificate as a base64 secret (e.g., `WINDOWS_CERT_BASE64`) and the
   password as `WINDOWS_CERT_PASSWORD`.
4. Sign the published executables with `signtool` (Windows SDK):
   ```powershell
   signtool sign /fd SHA256 /f cert.pfx /p $env:WINDOWS_CERT_PASSWORD `
     /tr http://timestamp.digicert.com /td SHA256 /v app.exe
   ```
   - `signtool` is available with the Windows SDK (winget: `Microsoft.Windows SDK`).
5. Verify: `signtool verify /pa /v app.exe`.

## macOS — Notarization (Tauri)

1. Apple Developer account + `Developer ID Application` certificate.
2. In CI, secrets: `APPLE_CERT_BASE64`, `APPLE_CERT_PASSWORD`, `APPLE_ID`, `APPLE_APP_PASSWORD` (app-specific).
3. Sign the `.app` bundle and then notarize:
   ```bash
   # extract cert to a keychain
   # sign
   codesign --force --options runtime --sign "$IDENTITY" --deep app.app
   # notarize
   xcrun notarytool submit app.zip --apple-id "$APPLE_ID" \
     --password "$APPLE_APP_PASSWORD" --team-id "$TEAM_ID" --wait
   # staple
   xcrun stapler staple app.app
   ```
   The Tauri `tauri.conf.json` `bundle.macOS` can embed signing settings when using
   `tauri build` with a `TAURI_SIGNING_PRIVATE_KEY` (for update signing) plus the
   Apple certificates.

## Linux

- No mandatory signing; `.deb`/`.rpm` can be signed with GPG (`dpkg-sig`, `rpm --addsign`).
- Store GPG key as `LINUX_GPG_KEY` + passphrase.

## CI wiring (future)

- Desktop/Tauri jobs: add signing steps after `dotnet publish`/`tauri build`, before
  artifact upload.
- Secrets to define in the repository (submodule GitHub settings):
  `WINDOWS_CERT_BASE64`, `WINDOWS_CERT_PASSWORD`, `APPLE_CERT_BASE64`,
  `APPLE_CERT_PASSWORD`, `APPLE_ID`, `APPLE_APP_PASSWORD`, `TEAM_ID`, `LINUX_GPG_KEY`.
- Do not commit certificate material or passphrases anywhere in the repo.

## References

- signtool docs: https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool
- notarytool docs: https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution
- Tauri signing: https://tauri.app/distribute/sign/