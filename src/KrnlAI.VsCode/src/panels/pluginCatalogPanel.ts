import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class PluginCatalogPanel {
    public static currentPanel: PluginCatalogPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private readonly _nonce: string;

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._client = new KernelClient();
        this._nonce = Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => { PluginCatalogPanel.currentPanel = undefined; });
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg));
    }

    static createOrShow() {
        if (PluginCatalogPanel.currentPanel) {
            PluginCatalogPanel.currentPanel._panel.reveal();
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            'krnlai.pluginCatalog', 'Krnl-AI - Plugin Catalog',
            vscode.ViewColumn.Beside,
            { enableScripts: true, retainContextWhenHidden: true }
        );
        PluginCatalogPanel.currentPanel = new PluginCatalogPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'load') {
            const plugins = await this._client.listPlugins();
            this._panel.webview.postMessage({ type: 'plugins', plugins });
        } else if (msg.type === 'install') {
            const ok = await this._client.installPlugin(msg.pluginId);
            this._panel.webview.postMessage({ type: 'installResult', pluginId: msg.pluginId, ok });
            if (ok) {
                const plugins = await this._client.listPlugins();
                this._panel.webview.postMessage({ type: 'plugins', plugins });
            }
        }
    }

    private _getHtml(): string {
        const nonce = this._nonce;
        return `<!DOCTYPE html>
<html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Plugin Catalog</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px}
h1{font-size:20px;margin-bottom:4px}p{color:var(--vscode-descriptionForeground);margin-bottom:24px;font-size:13px}
.plugin{background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;padding:16px;margin-bottom:12px;display:flex;justify-content:space-between;align-items:center}
.plugin-info{flex:1}.plugin-name{font-size:14px;font-weight:600}.plugin-version{font-size:11px;color:var(--vscode-descriptionForeground);margin-left:8px}
.plugin-desc{font-size:12px;color:var(--vscode-descriptionForeground);margin-top:4px}
button{padding:6px 14px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:12px}
button:hover{background:var(--vscode-button-hoverBackground)}button:disabled{opacity:.5;cursor:default}
#error{display:none;padding:12px;background:var(--vscode-inputValidation-errorBackground);border-radius:8px;margin-bottom:16px;color:var(--vscode-errorForeground);font-size:13px}
#loading{text-align:center;padding:40px;color:var(--vscode-descriptionForeground)}.toast{position:fixed;bottom:16px;left:50%;transform:translateX(-50%);padding:8px 16px;border-radius:6px;font-size:12px;z-index:999;transition:opacity .3s}
.toast.success{background:var(--vscode-testing-iconPassed);color:#fff}.toast.error{background:var(--vscode-testing-iconFailed);color:#fff}
</style></head><body>
<h1>📦 Plugin Catalog</h1>
<p>Browse and install Krnl-AI plugins.</p>
<div id="error"></div>
<div id="loading">Loading plugins...</div>
<div id="content" style="display:none"></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi();vscode.postMessage({type:'load'});
function showToast(msg,type){const t=document.createElement('div');t.className='toast '+type;t.textContent=msg;document.body.appendChild(t);setTimeout(()=>t.remove(),3000)}
window.addEventListener('message',e=>{const m=e.data;
if(m.type==='plugins'){
document.getElementById('loading')!.style.display='none';document.getElementById('content')!.style.display='block';
const c=document.getElementById('content')!;c.innerHTML='';
if(!m.plugins||m.plugins.length===0){c.innerHTML='<p style="color:var(--vscode-descriptionForeground)">No plugins available.</p>';return;}
for(const p of m.plugins){
const d=document.createElement('div');d.className='plugin';
d.innerHTML='<div class="plugin-info"><span class="plugin-name">'+p.name+'</span><span class="plugin-version">v'+p.version+'</span><div class="plugin-desc">'+(p.description||'')+'</div></div>';
const btn=document.createElement('button');btn.textContent='Install';btn.onclick=()=>{btn.disabled=true;btn.textContent='Installing...';vscode.postMessage({type:'install',pluginId:p.id})};
d.appendChild(btn);c.appendChild(d);}}
if(m.type==='installResult'){
if(m.ok)showToast('Installed: '+m.pluginId,'success');
else showToast('Failed to install: '+m.pluginId,'error');}
});
})();</script></body></html>`;
    }
}
