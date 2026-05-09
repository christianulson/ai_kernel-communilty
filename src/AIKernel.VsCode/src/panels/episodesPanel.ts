import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

const CSP = `<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'nonce-aikernel';">`;

export class EpisodesPanel {
    public static currentPanel: EpisodesPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._client = new KernelClient();
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (EpisodesPanel.currentPanel) { EpisodesPanel.currentPanel._panel.reveal(); return; }
        const panel = vscode.window.createWebviewPanel('aikernel.episodes', 'AI Kernel - Episódios', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        EpisodesPanel.currentPanel = new EpisodesPanel(panel);
    }

    private async _handle(msg: any) {
        if (msg.type === 'load') {
            const episodes = await this._client.getEpisodes();
            this._panel.webview.postMessage({ type: 'episodes', list: episodes || [] });
        }
        if (msg.type === 'detail') {
            const ep = await this._client.getEpisode(msg.id);
            this._panel.webview.postMessage({ type: 'detail', episode: ep });
        }
    }

    private _getHtml(): string { return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">${CSP}<title>Episódios</title>
<style>*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:24px}
h1{font-size:20px;margin-bottom:4px}p{color:var(--vscode-descriptionForeground);margin-bottom:16px;font-size:13px}
button{padding:8px 16px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px;margin-bottom:16px}
.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}
.ep{padding:12px;margin-bottom:8px;background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;cursor:pointer}
.ep:hover{background:var(--vscode-list-hoverBackground)}.ep .info{font-size:13px}.ep .status{font-size:11px;margin-top:4px}
.detail{background:var(--vscode-editor-inactiveSelectionBackground);border-radius:8px;padding:20px}
.detail h3{font-size:14px;margin-bottom:12px}.detail .row{font-size:13px;margin-bottom:8px;color:var(--vscode-descriptionForeground)}
.step{padding:8px;margin-bottom:4px;background:var(--vscode-editor-background);border-radius:4px;font-size:12px}
#detail{display:none}
</style></head><body>
<h1>📜 Episódios</h1><p>Histórico de execuções do agente.</p>
<button onclick="loadList()">↻ Atualizar</button>
<div class="grid"><div id="list"></div><div id="detail"></div></div>
<script nonce="aikernel">(function(){
const vscode=acquireVsCodeApi(),list=document.getElementById('list')!,detail=document.getElementById('detail')!;vscode.postMessage({type:'load'});
window.addEventListener('message',e=>{const m=e.data;
if(m.type==='episodes'){list.innerHTML='<h2>Lista</h2>';if(!m.list||m.list.length===0){list.innerHTML+='<p>Nenhum episódio.</p>';return;}
m.list.forEach((ep:any)=>{const d=document.createElement('div');d.className='ep';d.onclick=()=>vscode.postMessage({type:'detail',id:ep.id});
const i=document.createElement('div');i.className='info';i.textContent=(ep.status||'')+' - '+(ep.goalId||'');
d.appendChild(i);const s=document.createElement('div');s.className='status';s.textContent=ep.createdAt?new Date(ep.createdAt).toLocaleString('pt-BR'):'';
d.appendChild(s);list.appendChild(d);});}
if(m.type==='detail'&&m.episode){detail.style.display='block';
let html='<h3>Detalhe</h3><div class="row">Goal: '+escape(m.episode.goalId||'')+'</div><div class="row">Status: '+escape(m.episode.status||'')+'</div><div class="row">Duração: '+(m.episode.durationMs||'-')+'ms</div>';
if(m.episode.steps)html+=m.episode.steps.map((s:any)=>'<div class="step">'+(s.ok?'✔':'✖')+' '+escape(s.label||'')+': '+escape(s.detail||'')+'</div>').join('');
detail.innerHTML=html;}});
function escape(s){return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');}
function loadList(){detail.style.display='none';vscode.postMessage({type:'load'});}
})();</script></body></html>`; }

    public dispose() { EpisodesPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
