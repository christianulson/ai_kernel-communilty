import * as vscode from 'vscode';

export class SettingsPanel {
    public static currentPanel: SettingsPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _nonce: string;
    private _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._nonce = Math.random().toString(36).substring(2, 10) + Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (SettingsPanel.currentPanel) { SettingsPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('krnlai.settings', 'Krnl-AI - Configurações', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        SettingsPanel.currentPanel = new SettingsPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'load') {
            const config = vscode.workspace.getConfiguration('krnlai');
            this._panel.webview.postMessage({
                type: 'config',
                endpoint: config.get('endpoint'),
                mode: config.get('mode'),
                standalone: config.get('standalone'),
                sidecarPort: config.get('sidecarPort')
            });
        }
        if (msg.type === 'save') {
            if (typeof msg.endpoint !== 'string') return;
            const mode = msg.mode === 'embedded' || msg.mode === 'localApi' || msg.mode === 'remoteApi'
                ? msg.mode
                : (msg.standalone ? 'embedded' : 'localApi');
            try {
                const url = new URL(msg.endpoint);
                const isLoopback = url.hostname === 'localhost' || url.hostname === '127.0.0.1' || url.hostname === '::1';
                if (mode === 'localApi' && !isLoopback) {
                    vscode.window.showErrorMessage('Endpoint em modo API Local deve ser localhost, 127.0.0.1 ou ::1');
                    return;
                }
                if (mode === 'remoteApi' && url.protocol !== 'http:' && url.protocol !== 'https:') {
                    vscode.window.showErrorMessage('Endpoint remoto deve usar http ou https');
                    return;
                }
            } catch {
                vscode.window.showErrorMessage('URL inválida');
                return;
            }
            const port = Number(msg.sidecarPort);
            if (msg.sidecarPort !== undefined && (!Number.isInteger(port) || port < 1 || port > 65535)) {
                vscode.window.showErrorMessage('Porta inválida. Use 1-65535');
                return;
            }
            const config = vscode.workspace.getConfiguration('krnlai');
            const currentEndpoint = config.inspect('endpoint');
            if (currentEndpoint?.workspaceValue) {
                const action = await vscode.window.showWarningMessage(
                    'O endpoint está definido nas configurações do workspace (.vscode/settings.json) e sobrescreverá a configuração global. Deseja removê-lo do workspace?',
                    'Sim, remover do workspace', 'Ignorar'
                );
                if (action === 'Sim, remover do workspace') {
                    config.update('endpoint', undefined, vscode.ConfigurationTarget.Workspace);
                }
            }
            config.update('endpoint', msg.endpoint, vscode.ConfigurationTarget.Global);
            config.update('mode', mode, vscode.ConfigurationTarget.Global);
            config.update('standalone', mode === 'embedded', vscode.ConfigurationTarget.Global);
            config.update('sidecarPort', port || 5001, vscode.ConfigurationTarget.Global);
            vscode.window.showInformationMessage('Configurações salvas!');
        }
    }

    private _getHtml(): string { const nonce = this._nonce; return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Configurações</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px;max-width:500px}
h1{font-size:20px;margin-bottom:24px}h2{font-size:14px;font-weight:600;margin-bottom:12px;margin-top:24px}
label{display:block;margin-bottom:16px;font-size:13px}label span{display:block;margin-bottom:4px;color:var(--vscode-descriptionForeground)}
input,select{width:100%;padding:8px 12px;border:1px solid var(--vscode-input-border);background:var(--vscode-input-background);color:var(--vscode-input-foreground);border-radius:4px}
button{padding:10px 24px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px;margin-top:16px;width:100%}
.card{background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;padding:20px;margin-bottom:16px}
</style></head><body>
<h1>⚙️ Configurações</h1>
<div class="card"><h2>🔌 Conexão</h2>
<label><span>Modo</span><select id="mode"><option value="embedded">Embedded local (Sidecar)</option><option value="localApi">API Local</option><option value="remoteApi">API Remota</option></select></label>
<label><span>Endpoint</span><input type="text" id="endpoint" placeholder="http://localhost:5000" /></label>
<label><span>Porta Sidecar</span><input type="number" id="sidecarPort" /></label></div>
<button onclick="save()">💾 Salvar</button><div id="status"></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi();vscode.postMessage({type:'load'});
window.addEventListener('message',e=>{const m=e.data;if(m.type==='config'){
(document.getElementById('endpoint')as HTMLInputElement).value=m.endpoint||'http://localhost:5000';
(document.getElementById('sidecarPort')as HTMLInputElement).value=String(m.sidecarPort||5001);
(document.getElementById('mode')as HTMLSelectElement).value=m.mode||(m.standalone?'embedded':'localApi');}});
function save(){
const mode=(document.getElementById('mode')as HTMLSelectElement).value;
const endpoint=(document.getElementById('endpoint')as HTMLInputElement).value;
const port=parseInt((document.getElementById('sidecarPort')as HTMLInputElement).value)||5001;
vscode.postMessage({type:'save',endpoint,mode,standalone:mode==='embedded',sidecarPort:port});
document.getElementById('status')!.textContent='Salvo!';}
})();</script></body></html>`; }

    public dispose() { SettingsPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
