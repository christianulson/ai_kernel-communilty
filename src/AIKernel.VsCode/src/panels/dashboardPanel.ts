import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class DashboardPanel {
    public static currentPanel: DashboardPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private readonly _nonce: string;
    private _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._client = new KernelClient();
        this._nonce = Math.random().toString(36).substring(2, 10) + Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (DashboardPanel.currentPanel) { DashboardPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('aikernel.dashboard', 'AI Kernel - Dashboard', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        DashboardPanel.currentPanel = new DashboardPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'load') {
            const health = await this._client.health();
            const scorecard = await this._client.getScorecard();
            this._panel.webview.postMessage({ type: 'data', health, scorecard });
        }
    }

    private _getHtml(): string { const nonce = this._nonce; return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Dashboard</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px}
h1{font-size:20px;margin-bottom:4px}p{color:var(--vscode-descriptionForeground);margin-bottom:24px;font-size:13px}
.card{background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;padding:20px;margin-bottom:16px}
.card h2{font-size:14px;margin-bottom:16px;font-weight:600}.grid{display:grid;grid-template-columns:1fr 1fr 1fr 1fr 1fr;gap:8px;text-align:center}
.metric-value{font-size:24px;font-weight:700;color:var(--vscode-charts-green)}.metric-label{font-size:11px;color:var(--vscode-descriptionForeground);margin-top:4px}
.badge{display:inline-block;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:600}
.badge.online{background:var(--vscode-testing-iconPassed);color:#fff}.badge.offline{background:var(--vscode-testing-iconFailed);color:#fff}
button{padding:8px 16px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px;margin-bottom:16px}
button:hover{background:var(--vscode-button-hoverBackground)}#loading{text-align:center;padding:40px;color:var(--vscode-descriptionForeground)}
#error{display:none;padding:16px;background:var(--vscode-inputValidation-errorBackground);border-radius:8px;margin-bottom:16px;color:var(--vscode-errorForeground)}
</style></head><body>
<button onclick="load()">↻ Atualizar</button><div id="error"></div><div id="loading">Carregando...</div>
<div id="content" style="display:none">
<h1>📊 Dashboard</h1><p>Métricas, saúde do sistema e autonomia.</p>
<div class="card"><h2>🩺 Saúde do Sistema</h2>
<div class="grid"><div><div class="metric-value" id="gw-version">--</div><div class="metric-label">Gateway</div></div>
<div><div class="metric-value" id="kr-version">--</div><div class="metric-label">Kernel</div></div>
<div><div><span id="gw-status" class="badge">--</span></div><div class="metric-label">Status</div></div></div></div>
<div class="card"><h2>📈 Scorecard</h2>
<div class="grid">
<div><div class="metric-value" id="sc-reliability">--</div><div class="metric-label">Confiabilidade</div></div>
<div><div class="metric-value" id="sc-efficiency">--</div><div class="metric-label">Eficiência</div></div>
<div><div class="metric-value" id="sc-safety">--</div><div class="metric-label">Safety</div></div>
<div><div class="metric-value" id="sc-antiloop">--</div><div class="metric-label">Anti-Loop</div></div>
<div><div class="metric-value" id="sc-governance">--</div><div class="metric-label">Governança</div></div></div></div></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi();vscode.postMessage({type:'load'});
window.addEventListener('message',e=>{const m=e.data;
if(m.type==='data'){
document.getElementById('loading')!.style.display='none';document.getElementById('content')!.style.display='block';
if(m.health){document.getElementById('gw-version')!.textContent=m.health.version||'--';
document.getElementById('kr-version')!.textContent='v1.0.0';
const s=m.health.status==='ok'?'online':'offline';document.getElementById('gw-status')!.className='badge '+s;document.getElementById('gw-status')!.textContent=s;}
if(m.scorecard){document.getElementById('sc-reliability')!.textContent=Math.round(m.scorecard.reliability*100)+'%';
document.getElementById('sc-efficiency')!.textContent=Math.round(m.scorecard.efficiency*100)+'%';
document.getElementById('sc-safety')!.textContent=Math.round(m.scorecard.safety*100)+'%';
document.getElementById('sc-antiloop')!.textContent=Math.round(m.scorecard.antiLoop*100)+'%';
document.getElementById('sc-governance')!.textContent=Math.round(m.scorecard.governance*100)+'%';}
}});
function load(){document.getElementById('loading')!.style.display='block';document.getElementById('content')!.style.display='none';vscode.postMessage({type:'load'});}
})();</script></body></html>`; }

    public dispose() { DashboardPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
