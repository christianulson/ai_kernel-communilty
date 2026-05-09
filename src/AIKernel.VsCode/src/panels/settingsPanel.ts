import * as vscode from 'vscode';

const CSP = `<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'nonce-aikernel';">`;

export class SettingsPanel {
    public static currentPanel: SettingsPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (SettingsPanel.currentPanel) { SettingsPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('aikernel.settings', 'AI Kernel - Configurações', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        SettingsPanel.currentPanel = new SettingsPanel(panel);
    }

    private _handle(msg: any) {
        if (msg.type === 'load') {
            const config = vscode.workspace.getConfiguration('aikernel');
            this._panel.webview.postMessage({
                type: 'config',
                endpoint: config.get('endpoint'),
                standalone: config.get('standalone'),
                sidecarPort: config.get('sidecarPort')
            });
        }
        if (msg.type === 'save') {
            if (typeof msg.endpoint !== 'string' || !msg.endpoint.startsWith('http://') && !msg.endpoint.startsWith('https://')) {
                vscode.window.showErrorMessage('URL inválida. Use http:// ou https://');
                return;
            }
            const config = vscode.workspace.getConfiguration('aikernel');
            const currentEndpoint = config.inspect('endpoint');
            if (currentEndpoint?.workspaceValue && currentEndpoint.workspaceValue !== msg.endpoint) {
                vscode.window.showWarningMessage('O endpoint foi alterado nas configurações do workspace. Verifique o arquivo .vscode/settings.json');
            }
            config.update('endpoint', msg.endpoint, vscode.ConfigurationTarget.Global);
            config.update('standalone', !!msg.standalone, vscode.ConfigurationTarget.Global);
            config.update('sidecarPort', Number(msg.sidecarPort) || 5001, vscode.ConfigurationTarget.Global);
            vscode.window.showInformationMessage('Configurações salvas!');
        }
    }

    private _getHtml(): string { return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">${CSP}<title>Configurações</title>
<style>*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px;max-width:500px}
h1{font-size:20px;margin-bottom:24px}h2{font-size:14px;font-weight:600;margin-bottom:12px;margin-top:24px}
label{display:block;margin-bottom:16px;font-size:13px}label span{display:block;margin-bottom:4px;color:var(--vscode-descriptionForeground)}
input,select{width:100%;padding:8px 12px;border:1px solid var(--vscode-input-border);background:var(--vscode-input-background);color:var(--vscode-input-foreground);border-radius:4px}
button{padding:10px 24px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px;margin-top:16px;width:100%}
.card{background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;padding:20px;margin-bottom:16px}
</style></head><body>
<h1>⚙️ Configurações</h1>
<div class="card"><h2>🔌 Conexão</h2>
<label><span>Modo</span><select id="mode"><option value="remote">API Remota</option><option value="standalone">Standalone</option></select></label>
<label><span>Endpoint</span><input type="text" id="endpoint" placeholder="http://localhost:5000" /></label>
<label><span>Porta Sidecar</span><input type="number" id="sidecarPort" /></label></div>
<button onclick="save()">💾 Salvar</button><div id="status"></div>
<script nonce="aikernel">(function(){
const vscode=acquireVsCodeApi();vscode.postMessage({type:'load'});
window.addEventListener('message',e=>{const m=e.data;if(m.type==='config'){
(document.getElementById('endpoint')as HTMLInputElement).value=m.endpoint||'http://localhost:5000';
(document.getElementById('sidecarPort')as HTMLInputElement).value=String(m.sidecarPort||5001);
(document.getElementById('mode')as HTMLSelectElement).value=m.standalone?'standalone':'remote';}});
function save(){
const mode=(document.getElementById('mode')as HTMLSelectElement).value;
const endpoint=(document.getElementById('endpoint')as HTMLInputElement).value;
const port=parseInt((document.getElementById('sidecarPort')as HTMLInputElement).value)||5001;
vscode.postMessage({type:'save',endpoint,standalone:mode==='standalone',sidecarPort:port});
document.getElementById('status')!.textContent='Salvo!';}
})();</script></body></html>`; }

    public dispose() { SettingsPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
