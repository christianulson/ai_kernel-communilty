import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class MemoryPanel {
    public static currentPanel: MemoryPanel | undefined;
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
        if (MemoryPanel.currentPanel) { MemoryPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('krnlai.memory', 'AI Kernel - Memória', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        MemoryPanel.currentPanel = new MemoryPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'search') {
            const result = await this._client.searchMemory(msg.query);
            this._panel.webview.postMessage({ type: 'results', hits: result?.hits || [], total: result?.totalCount || 0 });
        }
        if (msg.type === 'metrics') {
            const metrics = await this._client.getMemoryMetrics();
            this._panel.webview.postMessage({ type: 'metrics', data: metrics });
        }
    }

    private _getHtml(): string { const nonce = this._nonce; return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Memória</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px}
h1{font-size:20px;margin-bottom:4px}p{color:var(--vscode-descriptionForeground);margin-bottom:16px;font-size:13px}
.tabs{display:flex;gap:8px;margin-bottom:16px}.tab{padding:8px 16px;border-radius:4px;cursor:pointer;font-size:13px;background:var(--vscode-editor-inactiveSelectionBackground)}
.tab.active{background:var(--vscode-button-background);color:var(--vscode-button-foreground)}
.search-area{display:flex;gap:8px;margin-bottom:16px}
input{flex:1;padding:8px 12px;border:1px solid var(--vscode-input-border);background:var(--vscode-input-background);color:var(--vscode-input-foreground);border-radius:4px}
button{padding:8px 16px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px}
.hit{padding:12px;margin-bottom:8px;background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;font-size:12px}
.hit .score-bar{height:6px;background:var(--vscode-testing-iconPassed);border-radius:3px;margin-bottom:8px}
.hit .text{color:var(--vscode-editor-foreground);margin-bottom:4px}.hit .source{font-size:11px;color:var(--vscode-descriptionForeground)}
.metrics-grid{display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px;margin-bottom:16px}
.metric{padding:16px;background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;text-align:center}
.metric .val{font-size:20px;font-weight:700;color:var(--vscode-charts-green)}.metric .lbl{font-size:11px;color:var(--vscode-descriptionForeground);margin-top:4px}
#search-page,#metrics-page{display:none}
</style></head><body>
<h1>🧠 Memória</h1><p>Busca semântica e métricas.</p>
<div class="tabs"><div class="tab active" id="tab-search" onclick="showTab('search')">🔍 Busca</div><div class="tab" id="tab-metrics" onclick="showTab('metrics')">📊 Métricas</div></div>
<div id="search-page"><div class="search-area"><input id="query" placeholder="Digite sua busca..." /><button onclick="search()">Buscar</button></div><div id="results"></div></div>
<div id="metrics-page"><button onclick="loadMetrics()">↻ Atualizar</button>
<div class="metrics-grid"><div class="metric"><div class="val" id="m-chunks">--</div><div class="lbl">Chunks</div></div>
<div class="metric"><div class="val" id="m-docs">--</div><div class="lbl">Documentos</div></div>
<div class="metric"><div class="val" id="m-size">--</div><div class="lbl">Bytes</div></div></div></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi(),results=document.getElementById('results')!;
window.addEventListener('message',e=>{const m=e.data;
if(m.type==='results'){results.innerHTML='';if(m.total===0){results.textContent='Nenhum resultado.';return;}
m.hits.forEach((h:any)=>{const d=document.createElement('div');d.className='hit';
const b=document.createElement('div');b.className='score-bar';b.style.width=Math.round(h.score*100)+'%';d.appendChild(b);
const t=document.createElement('div');t.className='text';t.textContent=h.content||'';d.appendChild(t);
const s=document.createElement('div');s.className='source';s.textContent='Score: '+Math.round(h.score*100)+'%';d.appendChild(s);
results.appendChild(d);});}
if(m.type==='metrics'&&m.data){document.getElementById('m-chunks')!.textContent=String(m.data.totalChunks||0);
document.getElementById('m-docs')!.textContent=String(m.data.totalDocuments||0);
document.getElementById('m-size')!.textContent=Number(m.data.totalSizeBytes||0).toLocaleString();}
});
function search(){const q=(document.getElementById('query')as HTMLInputElement).value;if(!q)return;vscode.postMessage({type:'search',query:q});}
function loadMetrics(){vscode.postMessage({type:'metrics'});}
function showTab(t){['search','metrics'].forEach(x=>{const e=document.getElementById('tab-'+x)!;e.className=x===t?'tab active':'tab';document.getElementById(x+'-page')!.style.display=x===t?'block':'none';});
if(t==='metrics')loadMetrics();}
showTab('search');
})();</script></body></html>`; }

    public dispose() { MemoryPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
