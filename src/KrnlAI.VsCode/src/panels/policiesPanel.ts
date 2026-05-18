import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class PoliciesPanel {
    public static currentPanel: PoliciesPanel | undefined;
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
        if (PoliciesPanel.currentPanel) { PoliciesPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('krnlai.policies', 'Krnl-AI - Políticas', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        PoliciesPanel.currentPanel = new PoliciesPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'load') {
            const policies = await this._client.getPolicies(msg.domain);
            this._panel.webview.postMessage({ type: 'policies', list: policies || [] });
        }
    }

    private _getHtml(): string { const nonce = this._nonce; return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Políticas</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px}
h1{font-size:20px;margin-bottom:4px}p{color:var(--vscode-descriptionForeground);margin-bottom:16px;font-size:13px}
select{width:100%;padding:8px;margin-bottom:16px;background:var(--vscode-input-background);color:var(--vscode-input-foreground);border:1px solid var(--vscode-input-border);border-radius:4px}
button{padding:8px 16px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px;margin-bottom:16px}
button:hover{background:var(--vscode-button-hoverBackground)}
.policy{padding:12px;margin-bottom:8px;background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px}
.policy .name{font-weight:600;font-size:13px}.policy .meta{font-size:11px;color:var(--vscode-descriptionForeground);margin-top:4px}
</style></head><body>
<h1>📋 Políticas</h1><p>Políticas aprendidas, versões e rollbacks.</p>
<select id="domain"><option value="">Todos</option><option value="general">General</option><option value="payments">Payments</option><option value="security">Security</option></select>
<button onclick="load()">↻ Atualizar</button><div id="list"></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi(),list=document.getElementById('list')!;vscode.postMessage({type:'load',domain:''});
document.getElementById('domain')!.onchange=load;
window.addEventListener('message',e=>{const m=e.data;if(m.type!=='policies')return;list.innerHTML='';
if(!m.list||m.list.length===0){list.textContent='Nenhuma política encontrada.';return;}
m.list.forEach((p:any)=>{const div=document.createElement('div');div.className='policy';
const n=document.createElement('div');n.className='name';n.textContent=p.name;div.appendChild(n);
const m2=document.createElement('div');m2.className='meta';m2.textContent=p.domain+' • v'+p.version;div.appendChild(m2);
list.appendChild(div);});});
function load(){const d=(document.getElementById('domain')as HTMLSelectElement).value;vscode.postMessage({type:'load',domain:d});}
})();</script></body></html>`; }

    public dispose() { PoliciesPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
