import * as vscode from 'vscode';
import { KernelClient } from '../api/client';
import { escapeHtml } from '../utils/escapeHtml';

export class ChatPanel {
    public static currentPanel: ChatPanel | undefined;
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
        this._panel.webview.onDidReceiveMessage(msg => this._handleMessage(msg), null, this._disposables);
    }

    static createOrShow() {
        if (ChatPanel.currentPanel) { ChatPanel.currentPanel._panel.reveal(vscode.ViewColumn.Beside); return; }
        const panel = vscode.window.createWebviewPanel('krnlai.chat', 'Krnl-AI - Chat', vscode.ViewColumn.Beside, { enableScripts: true, retainContextWhenHidden: true });
        ChatPanel.currentPanel = new ChatPanel(panel);
    }

    private async _handleMessage(msg: any) {
        if (msg.type === 'send') {
            const response = await this._client.runAgent(msg.text);
            this._panel.webview.postMessage({
                type: 'response',
                data: response.narration || response.error || 'Sem resposta',
                error: response.error
            });
        }
        if (msg.type === 'checkHealth') {
            const [status, emotional] = await Promise.all([
                this._client.getStatusMessage(),
                this._client.getEmotionalState()
            ]);
            const mood = emotional
                ? (emotional.valence > 0.3
                    ? (emotional.arousal < 0.4 ? '😌 Tranquilo' : '⚡ Animado')
                    : emotional.valence < -0.3
                        ? (emotional.arousal < 0.4 ? '😮‍💨 Cansado' : '😰 Tenso')
                        : emotional.arousal >= 0.4 ? '🧐 Atento' : '😐 Neutro')
                : '';
            this._panel.webview.postMessage({ type: 'health', status, mood });
        }
    }

    private _getHtml(): string { const nonce = this._nonce; return `<!DOCTYPE html>
<html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Krnl-AI Chat</title>
<style nonce="${nonce}">*{margin:0;padding:0;box-sizing:border-box}body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground)}
#status{padding:8px 16px;font-size:12px;border-bottom:1px solid var(--vscode-panel-border)}#messages{padding:16px;overflow-y:auto;height:calc(100vh - 120px)}
.msg{margin:0 0 12px;padding:12px;border-radius:8px;max-width:85%}.user{background:var(--vscode-textBlockQuote-background);margin-left:auto}
.assistant{background:var(--vscode-editor-inactiveSelectionBackground)}.msg-label{font-size:11px;opacity:0.7;margin-bottom:4px}.error{color:var(--vscode-errorForeground)}
#input-area{display:flex;padding:12px 16px;gap:8px;border-top:1px solid var(--vscode-panel-border)}
#input{flex:1;padding:8px 12px;border:1px solid var(--vscode-input-border);background:var(--vscode-input-background);color:var(--vscode-input-foreground);border-radius:4px}
#send{padding:8px 20px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer}
#send:hover{background:var(--vscode-button-hoverBackground)}
</style></head><body>
<div id="status">$(sync) Conectando...</div><div id="messages"></div>
<div id="input-area"><input id="input" type="text" placeholder="Digite sua mensagem..." /><button id="send">Enviar</button></div>
<script nonce="${nonce}">(function(){
const vscode=acquireVsCodeApi(),msgDiv=document.getElementById('messages')!,input=document.getElementById('input')as HTMLInputElement,status=document.getElementById('status')!;
vscode.postMessage({type:'checkHealth'});
window.addEventListener('message',e=>{const m=e.data;
if(m.type==='health')status.textContent=m.mood?m.status+' · '+m.mood:m.status;
if(m.type==='response'){addMsg(m.data,'assistant',m.error?'error':'');if(m.error)status.textContent='$(error) '+m.error;}
});
document.getElementById('send')!.onclick=send;input.onkeydown=e=>{if(e.key==='Enter')send();};
function send(){const t=input.value.trim();if(!t)return;addMsg(t,'user');vscode.postMessage({type:'send',text:t});input.value='';status.textContent='$(sync) Processando...';}
function addMsg(t,r,e){const d=document.createElement('div');d.className='msg '+r+' '+e;
const l=document.createElement('div');l.className='msg-label';l.textContent=r==='user'?'Usuário':'Krnl-AI';d.appendChild(l);
const c=document.createElement('div');c.textContent=t;d.appendChild(c);msgDiv.appendChild(d);d.scrollIntoView({behavior:'smooth'});}
})();</script></body></html>`; }

    public dispose() { ChatPanel.currentPanel = undefined; this._panel.dispose(); while (this._disposables.length) this._disposables.pop()!.dispose(); }
}
