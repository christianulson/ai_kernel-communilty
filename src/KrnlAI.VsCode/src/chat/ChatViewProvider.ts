import * as path from 'path';
import * as vscode from 'vscode';
import { KernelClient } from '../api/client';
import { EditorContextProvider } from '../codingAgent/EditorContextProvider';
import { SlashCommandManager } from '../codingAgent/SlashCommandManager';
import { ApprovalManager, ApprovalMode } from '../codingAgent/ApprovalManager';
import { ChatMessage, createMessage } from './ChatMessage';
import { escapeHtml } from '../utils/escapeHtml';
import { SessionManager } from '../services/SessionManager';
import { UsageTracker } from '../services/UsageTracker';

const MAX_MESSAGES = 50;

export class ChatViewProvider {
    public static currentPanel: ChatViewProvider | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private readonly _approvalManager: ApprovalManager;
    private readonly _sessionManager?: SessionManager;
    private readonly _usageTracker?: UsageTracker;
    private _context: EditorContextProvider | null = null;
    private readonly _slashCommands: SlashCommandManager;
    private readonly _nonce: string;
    private _messages: ChatMessage[] = [];
    private _disposables: vscode.Disposable[] = [];
    private _editorSubscriptions: vscode.Disposable[] = [];
    private _currentSessionId: string | undefined;

    private constructor(panel: vscode.WebviewPanel, client: KernelClient, approvalManager: ApprovalManager, sessionManager?: SessionManager, usageTracker?: UsageTracker) {
        this._panel = panel;
        this._client = client;
        this._approvalManager = approvalManager;
        this._sessionManager = sessionManager;
        this._usageTracker = usageTracker;
        this._slashCommands = new SlashCommandManager(client);
        this._nonce = Math.random().toString(36).substring(2, 10) + Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handleMessage(msg), null, this._disposables);

        this._editorSubscriptions.push(
            vscode.window.onDidChangeActiveTextEditor(() => this._pushContext()),
            vscode.languages.onDidChangeDiagnostics(() => {
                this._context?.invalidateDiagCache();
                this._pushContext();
            })
        );

        this._approvalManager.onPending(approval => {
            this._panel.webview.postMessage({
                type: 'approval',
                id: approval.id,
                action: approval.action,
                details: approval.details,
                deadline: approval.deadline,
                remaining: Math.max(0, approval.deadline - Date.now())
            });
        });

        this._loadLastSession();
    }

    static createOrShow(client?: KernelClient, approvalManager?: ApprovalManager, sessionManager?: SessionManager, usageTracker?: UsageTracker) {
        if (ChatViewProvider.currentPanel) {
            ChatViewProvider.currentPanel._panel.reveal(vscode.ViewColumn.Beside);
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            'krnlai.coding.chat',
            'Krnl-AI - Coding Agent',
            vscode.ViewColumn.Beside,
            { enableScripts: true, retainContextWhenHidden: true }
        );
        ChatViewProvider.currentPanel = new ChatViewProvider(panel, client || new KernelClient(), approvalManager || new ApprovalManager(), sessionManager, usageTracker);
    }

    private _ensureContext(): EditorContextProvider {
        if (!this._context) this._context = new EditorContextProvider();
        return this._context;
    }

    private async _pushContext(): Promise<void> {
        const ctx = await this._ensureContext().getFullContext();
        this._panel.webview.postMessage({ type: 'context', context: ctx });
    }

    private _trimMessages(): void {
        if (this._messages.length > MAX_MESSAGES) {
            this._messages.splice(0, this._messages.length - MAX_MESSAGES);
        }
    }

    private async _handleMessage(msg: any) {
        if (msg.type === 'getContext') {
            await this._pushContext();
            return;
        }

        if (msg.type === 'send') {
            const input = msg.text as string;
            const ctx = await this._ensureContext().getFullContext();

            const userMsg = createMessage('user', input);
            this._messages.push(userMsg);
            this._trimMessages();
            this._panel.webview.postMessage({
                type: 'message',
                message: userMsg
            });

            const parsed = this._slashCommands.parse(input);
            let responseContent: string;

            if (parsed.command) {
                if (parsed.command === '/sessions' || parsed.command === '/session' || parsed.command === '/export' || parsed.command === '/import' || parsed.command === '/stats') {
                    responseContent = await this._handleSessionCommand(parsed.command, parsed.args);
                } else {
                    try {
                        responseContent = await this._slashCommands.execute(input, ctx);
                        this._usageTracker?.trackCommand(parsed.command);
                    } catch (err: any) {
                        responseContent = `Erro: ${err.message}`;
                    }
                }
            } else {
                const contextPrefix = ctx.activeFile
                    ? `[Contexto: ${path.basename(ctx.activeFile)} (${ctx.language})]\n${ctx.selection ? 'Seleção ativa\n' : ''}`
                    : '';
                const prompt = contextPrefix
                    ? `${contextPrefix}\n\n${input}`
                    : input;

                const response = await this._client.runAgent(prompt);
                responseContent = response.narration || response.error || 'Sem resposta';
            }

            const assistantMsg = createMessage('assistant', responseContent, {
                command: parsed.command
            });
            this._messages.push(assistantMsg);
            this._trimMessages();
            this._panel.webview.postMessage({
                type: 'message',
                message: assistantMsg
            });
            this._panel.webview.postMessage({ type: 'done' });
            this._autoSave();
            return;
        }

        if (msg.type === 'getSlashCommands') {
            this._panel.webview.postMessage({
                type: 'slashCommands',
                commands: this._slashCommands.getAll().map(c => ({ id: c.id, description: c.description }))
            });
        }

        if (msg.type === 'approvalResponse') {
            this._approvalManager.respond(msg.id, msg.decision);
        }

        if (msg.type === 'setApprovalMode') {
            const mode = msg.mode as ApprovalMode;
            if (Object.values(ApprovalMode).includes(mode)) {
                this._approvalManager.setMode(mode);
                this._panel.webview.postMessage({ type: 'modeChanged', mode });
            }
        }
    }

    private _getHtml(): string {
        const nonce = this._nonce;
        const MAX_DOM_MSGS = 50;
        return `<!DOCTYPE html>
<html lang="${navigator.language || 'en'}">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Krnl-AI Coding Agent</title>
<style nonce="${nonce}">
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);display:flex;flex-direction:column;height:100vh;overflow:hidden}
#context-bar{padding:4px 12px;font-size:11px;background:var(--vscode-sideBarSectionHeader-background);border-bottom:1px solid var(--vscode-panel-border);display:flex;gap:8px;align-items:center;min-height:24px}
#context-bar .file{color:var(--vscode-textLink-foreground)}
#context-bar .lang{opacity:0.7}
#context-bar .diag-warn{color:var(--vscode-editorWarning-foreground)}
#context-bar .diag-err{color:var(--vscode-errorForeground)}
#context-bar .no-file{opacity:0.5;font-style:italic}
#messages{flex:1;overflow-y:auto;padding:12px 16px}
.msg{margin:0 0 10px;padding:10px 14px;border-radius:8px;max-width:88%;white-space:pre-wrap;word-wrap:break-word;line-height:1.5}
.msg.user{background:var(--vscode-textBlockQuote-background);margin-left:auto}
.msg.assistant{background:var(--vscode-editor-inactiveSelectionBackground)}
.msg.system{background:var(--vscode-editorWidget-background);border:1px solid var(--vscode-panel-border);margin:0 auto;max-width:70%;text-align:center;font-size:12px;opacity:0.8}
.msg.error{background:var(--vscode-inputValidation-errorBackground);border:1px solid var(--vscode-inputValidation-errorBorder)}
.msg .label{font-size:10px;opacity:0.6;margin-bottom:4px;text-transform:uppercase;letter-spacing:0.5px}
.msg .content{font-size:13px}
.msg .content code{background:var(--vscode-textCodeBlock-background);padding:1px 4px;border-radius:3px;font-size:12px}
.msg .content pre{background:var(--vscode-textCodeBlock-background);padding:10px;border-radius:6px;overflow-x:auto;margin:8px 0;font-size:12px}
.msg .content pre code{background:none;padding:0}
#context-area{max-height:120px;overflow-y:auto;padding:6px 12px;background:var(--vscode-sideBarSectionHeader-background);border-top:1px solid var(--vscode-panel-border);font-size:11px;display:none}
#context-area .ctx-item{padding:2px 0;display:flex;gap:6px}
#context-area .ctx-item .key{font-weight:600;min-width:70px}
#context-area .ctx-item .val{opacity:0.8;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
#input-area{display:flex;padding:8px 12px;gap:6px;border-top:1px solid var(--vscode-panel-border);align-items:center}
#input-wrapper{flex:1;position:relative}
#input{width:100%;padding:8px 12px;border:1px solid var(--vscode-input-border);background:var(--vscode-input-background);color:var(--vscode-input-foreground);border-radius:4px;font-size:13px;font-family:inherit}
#input:focus{outline:none;border-color:var(--vscode-focusBorder)}
#autocomplete{position:absolute;bottom:100%;left:0;right:0;background:var(--vscode-dropdown-background);border:1px solid var(--vscode-dropdown-border);border-radius:4px;display:none;max-height:180px;overflow-y:auto;margin-bottom:2px;z-index:10}
.autocomplete-item{padding:6px 10px;cursor:pointer;font-size:12px;display:flex;justify-content:space-between;gap:8px}
.autocomplete-item:hover,.autocomplete-item.active{background:var(--vscode-list-hoverBackground)}
.autocomplete-item .desc{opacity:0.6;font-size:11px}
#send{padding:8px 16px;background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;border-radius:4px;cursor:pointer;font-size:13px}
#send:hover{background:var(--vscode-button-hoverBackground)}
#send:disabled{opacity:0.5;cursor:default}
#status-area{display:flex;gap:8px;align-items:center;padding:2px 12px;font-size:11px;border-top:1px solid var(--vscode-panel-border);min-height:20px}
#status-area .dot{width:6px;height:6px;border-radius:50%;display:inline-block}
.dot.idle{background:var(--vscode-testing-iconPassed)}
.dot.running{background:var(--vscode-testing-iconQueued);animation:pulse 1s infinite}
.dot.error{background:var(--vscode-testing-iconFailed)}
@keyframes pulse{0%,100%{opacity:1}50%{opacity:0.4}}
#status-text{opacity:0.6}
#mode-selector{display:flex;gap:4px;align-items:center;margin-left:auto;font-size:10px}
#mode-selector select{font-size:10px;padding:1px 4px;background:var(--vscode-dropdown-background);color:var(--vscode-dropdown-foreground);border:1px solid var(--vscode-dropdown-border);border-radius:2px}
#approval-overlay{position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.5);display:none;z-index:100;justify-content:center;align-items:center}
#approval-modal{background:var(--vscode-editorWidget-background);border:1px solid var(--vscode-panel-border);border-radius:8px;padding:16px;max-width:400px;width:90%;box-shadow:0 4px 12px rgba(0,0,0,0.3)}
#approval-modal h3{margin:0 0 8px;font-size:14px}
#approval-modal .action{font-size:13px;margin-bottom:4px}
#approval-modal .details{margin:8px 0;padding:8px;background:var(--vscode-textBlockQuote-background);border-radius:4px;font-size:12px;max-height:120px;overflow-y:auto}
#approval-modal .deadline{font-size:11px;opacity:0.7;margin-bottom:8px}
#approval-modal .buttons{display:flex;gap:8px;justify-content:flex-end}
#approval-modal button{padding:6px 16px;border:none;border-radius:4px;cursor:pointer;font-size:12px}
#approval-modal .btn-allow{background:var(--vscode-testing-iconPassed);color:var(--vscode-button-foreground)}
#approval-modal .btn-reject{background:var(--vscode-testing-iconFailed);color:white}
#approval-modal .btn-allow:hover{opacity:0.8}
#approval-modal .btn-reject:hover{opacity:0.8}
</style></head>
<body>
<div id="context-bar"><span id="ctx-file" class="no-file">Nenhum arquivo ativo</span></div>
<div id="messages"></div>
<div id="context-area"></div>
<div id="input-area">
<div id="input-wrapper">
<input id="input" type="text" placeholder="Digite / para comandos, ou envie uma mensagem..." autofocus />
<div id="autocomplete"></div>
</div>
<button id="send" disabled>Enviar</button>
</div>
<div id="status-area"><span class="dot idle" id="status-dot"></span><span id="status-text">Conectando...</span>
<div id="mode-selector">
<select id="modeSelect"><option value="chat">💬 Chat</option><option value="safeAgent">🛡️ Safe</option><option value="fullAgent">🤖 Full</option></select>
</div>
</div>
<div id="approval-overlay"><div id="approval-modal">
<h3>🔒 Aprovação necessária</h3>
<div class="action" id="app-action"></div>
<div class="details" id="app-details"></div>
<div class="deadline" id="app-deadline"></div>
<div class="buttons">
<button class="btn-reject" id="app-reject">Rejeitar</button>
<button class="btn-allow" id="app-allow">Permitir</button>
</div>
</div></div>
<script nonce="${nonce}">
(function(){const vscode=acquireVsCodeApi();
const msgDiv=document.getElementById('messages');
const input=document.getElementById('input');
const sendBtn=document.getElementById('send');
const ctxFile=document.getElementById('ctx-file');
const ctxArea=document.getElementById('context-area');
const statusText=document.getElementById('status-text');
const statusDot=document.getElementById('status-dot');
const autocomplete=document.getElementById('autocomplete');
let slashCommands=[];
let autocompleteIndex=-1;
const MAX_MSGS=${MAX_DOM_MSGS};
let currentApprovalId=null;
let approvalTimer=null;

function setStatus(text,type){statusText.textContent=text;statusDot.className='dot '+(type||'idle');}

function showApprovalModal(action,details,deadline,id){
currentApprovalId=id;
document.getElementById('app-action').textContent=action;
document.getElementById('app-details').textContent=details.join('\\n');
document.getElementById('approval-overlay').style.display='flex';
const tick=function(){const r=Math.max(0,deadline-Date.now());
document.getElementById('app-deadline').textContent='Expira em '+(r/1000).toFixed(0)+'s';
if(r<=0){hideApprovalModal();}};
tick();
approvalTimer=setInterval(tick,1000);}
function hideApprovalModal(){
document.getElementById('approval-overlay').style.display='none';
currentApprovalId=null;
if(approvalTimer){clearInterval(approvalTimer);approvalTimer=null;}}
document.getElementById('app-allow').onclick=function(){
if(currentApprovalId){vscode.postMessage({type:'approvalResponse',id:currentApprovalId,decision:'allowed'});}
hideApprovalModal();};
document.getElementById('app-reject').onclick=function(){
if(currentApprovalId){vscode.postMessage({type:'approvalResponse',id:currentApprovalId,decision:'rejected'});}
hideApprovalModal();};

document.getElementById('modeSelect').onchange=function(){
vscode.postMessage({type:'setApprovalMode',mode:this.value});};

vscode.postMessage({type:'getContext'});
vscode.postMessage({type:'getSlashCommands'});

window.addEventListener('message',e=>{const m=e.data;
if(m.type==='context'){const c=m.context;
const parts=[];
if(c.activeFile){const fileName=c.activeFile.split(/[\\\\/]/).pop()||c.activeFile;parts.push('<span class="file">'+escapeHtml(fileName)+'</span>');}
if(c.language){parts.push('<span class="lang">'+escapeHtml(c.language)+'</span>');}
if(c.selection){parts.push('<span style="opacity:0.6">(seleção ativa)</span>');}
const errCount=c.diagnostics.filter(d=>d.severity==='error').length;
const warnCount=c.diagnostics.filter(d=>d.severity==='warning').length;
if(errCount>0)parts.push('<span class="diag-err">'+errCount+' erro(s)</span>');
if(warnCount>0)parts.push('<span class="diag-warn">'+warnCount+' warning(s)</span>');
ctxFile.innerHTML=parts.length?parts.join(' \u00b7 '):'<span class="no-file">Nenhum arquivo ativo</span>';
if(c.activeFile){ctxArea.style.display='block';ctxArea.innerHTML='<div class="ctx-item"><span class="key">Arquivo:</span><span class="val">'+escapeHtml(c.activeFile)+'</span></div>';}
else{ctxArea.style.display='none';}
setStatus('Conectado','idle');}
if(m.type==='message'){addMessage(m.message);}
if(m.type==='slashCommands'){slashCommands=m.commands;}
if(m.type==='done'){sendBtn.disabled=false;setStatus('Conectado','idle');}
if(m.type==='approval'){showApprovalModal(m.action,m.details,m.deadline,m.id);}
if(m.type==='modeChanged'){document.getElementById('modeSelect').value=m.mode;}});

function trimMessages(){while(msgDiv.children.length>MAX_MSGS){msgDiv.removeChild(msgDiv.firstChild);}}

function addMessage(msg){const div=document.createElement('div');
div.className='msg '+(msg.isError?'error':msg.role);
const label=document.createElement('div');label.className='label';
label.textContent=msg.role==='user'?('Voc\u00ea'+(msg.metadata?.command?' ('+msg.metadata.command+')':'')):'Krnl-AI';
div.appendChild(label);
const content=document.createElement('div');content.className='content';
if(msg.role==='assistant'){content.innerHTML=renderMarkdown(escapeHtml(msg.content));}
else{content.textContent=msg.content;}
div.appendChild(content);
msgDiv.appendChild(div);
trimMessages();
div.scrollIntoView({behavior:'smooth',block:'end'});}

function renderMarkdown(text){let out=text;
out=out.replace(/^### (.+)$/gm,'<b>$1</b>');
out=out.replace(/^## (.+)$/gm,'<b>$1</b>');
out=out.replace(/^# (.+)$/gm,'<b>$1</b>');
out=out.replace(/\*\*(.+?)\*\*/g,'<b>$1</b>');
out=out.replace(/\*(.+?)\*/g,'<em>$1</em>');
out=out.replace(/\x60([^\x60]+)\x60/g,'<code>$1</code>');
out=out.replace(/\x60\x60\x60(\w*)\n([\s\S]*?)\x60\x60\x60/g,function(_,lang,code){return'<pre'+(lang?' data-lang="'+lang+'"':'')+'><code>'+code.replace(/</g,'&lt;').replace(/>/g,'&gt;')+'</code></pre>';});
out=out.replace(/^- (.+)$/gm,'\u2022 $1');
out=out.replace(/\n/g,'<br>');
return out;}

function escapeHtml(s){return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');}

function updateAutocomplete(){const text=input.value.substring(0,input.selectionStart||0);
const match=text.match(/(^|\s)(\/\w*)$/);
if(!match||!slashCommands.length){autocomplete.style.display='none';autocompleteIndex=-1;return;}
const prefix=match[2].toLowerCase();
const filtered=slashCommands.filter(c=>c.id.startsWith(prefix));
if(!filtered.length){autocomplete.style.display='none';autocompleteIndex=-1;return;}
autocomplete.textContent='';
filtered.forEach((cmd,i)=>{const item=document.createElement('div');
item.className='autocomplete-item'+(i===0?' active':'');
const idSpan=document.createElement('span');idSpan.textContent=cmd.id;
const descSpan=document.createElement('span');descSpan.className='desc';descSpan.textContent=cmd.description;
item.appendChild(idSpan);item.appendChild(descSpan);
item.onclick=function(){insertCommand(cmd.id);autocomplete.style.display='none';};
autocomplete.appendChild(item);});
autocomplete.style.display='block';
autocompleteIndex=filtered.length>0?0:-1;}

function insertCommand(cmd){const text=input.value;const caret=input.selectionStart||0;
const before=text.substring(0,caret);const after=text.substring(caret);
const match=before.match(/(^|\s)(\/\w*)$/);
if(match){const start=match.index||0;input.value=text.substring(0,start)+match[1]+cmd+' '+after;}
else{input.value=text.substring(0,caret)+cmd+' '+text.substring(caret);}
const pos=caret+cmd.length+1;input.setSelectionRange(pos,pos);input.focus();autocomplete.style.display='none';}

input.addEventListener('input',function(){sendBtn.disabled=!this.value.trim();updateAutocomplete();});
input.addEventListener('keydown',function(e){
if(autocomplete.style.display==='block'){
const items=autocomplete.querySelectorAll('.autocomplete-item');
if(e.key==='ArrowDown'){e.preventDefault();autocompleteIndex=Math.min(autocompleteIndex+1,items.length-1);
items.forEach((el,i)=>el.className='autocomplete-item'+(i===autocompleteIndex?' active':''));
return;}
if(e.key==='ArrowUp'){e.preventDefault();autocompleteIndex=Math.max(autocompleteIndex-1,0);
items.forEach((el,i)=>el.className='autocomplete-item'+(i===autocompleteIndex?' active':''));
return;}
if(e.key==='Enter'&&autocompleteIndex>=0){e.preventDefault();const active=items[autocompleteIndex];if(active)active.click();return;}
if(e.key==='Escape'){autocomplete.style.display='none';autocompleteIndex=-1;return;}}
if(e.key==='Enter'&&!e.shiftKey){e.preventDefault();send();}});

document.addEventListener('click',function(){autocomplete.style.display='none';});

sendBtn.onclick=send;
function send(){const t=input.value.trim();if(!t||sendBtn.disabled)return;
sendBtn.disabled=true;input.value='';autocomplete.style.display='none';
setStatus('Processando...','running');
vscode.postMessage({type:'send',text:t});}
})();
</script></body></html>`;
    }

    public dispose() {
        ChatViewProvider.currentPanel = undefined;
        this._panel.dispose();
        while (this._disposables.length) this._disposables.pop()!.dispose();
        while (this._editorSubscriptions.length) this._editorSubscriptions.pop()!.dispose();
    }

    private async _autoSave(): Promise<void> {
        if (!this._sessionManager) return;
        const label = `Chat ${new Date().toLocaleDateString()}`;
        this._currentSessionId = await this._sessionManager.autoSave(
            label, this._messages, this._currentSessionId
        );
    }

    private async _loadLastSession(): Promise<void> {
        if (!this._sessionManager) return;
        const sessions = await this._sessionManager.listSessions();
        if (sessions.length === 0) return;

        const last = sessions[0];
        this._currentSessionId = last.id;
        this._messages = last.messages.slice();

        for (const msg of this._messages) {
            this._panel.webview.postMessage({ type: 'message', message: msg });
        }
        this._panel.webview.postMessage({ type: 'done' });
    }

    private async _handleSessionCommand(command: string, args: string): Promise<string> {
        switch (command) {
            case '/sessions':
            case '/session':
            case '/export':
            case '/import': {
                if (!this._sessionManager) return 'Gerenciador de sessões não disponível';

                switch (command) {
                    case '/sessions': {
                        const sessions = await this._sessionManager.listSessions();
                        if (sessions.length === 0) return 'Nenhuma sessão salva.';
                        return sessions.map((s, i) =>
                            `${i + 1}. ${s.label} (${s.messageCount} msgs) - ${new Date(s.updatedAt).toLocaleString()}`
                        ).join('\n');
                    }

                    case '/session': {
                        const idx = parseInt(args) - 1;
                        const sessions = await this._sessionManager.listSessions();
                        if (isNaN(idx) || idx < 0 || idx >= sessions.length)
                            return 'Use /session <número>. Execute /sessions para listar.';
                        const session = sessions[idx];
                        this._currentSessionId = session.id;
                        this._messages = session.messages.slice();
                        return `✅ Sessão "${session.label}" carregada (${session.messageCount} mensagens).`;
                    }

                    case '/export': {
                        if (this._currentSessionId) {
                            const json = await this._sessionManager.exportSession(this._currentSessionId);
                            if (json) return `📤 Sessão exportada:\n\`\`\`json\n${json.substring(0, 1500)}\n\`\`\``;
                        }
                        return 'Nenhuma sessão ativa para exportar. Salve mensagens primeiro.';
                    }

                    case '/import': {
                        if (!args.startsWith('{')) return 'Cole o JSON da sessão após /import.';
                        const session = await this._sessionManager.importSession(args);
                        if (session) {
                            this._currentSessionId = session.id;
                            this._messages = session.messages.slice();
                            return `✅ Sessão "${session.label}" importada (${session.messageCount} mensagens).`;
                        }
                        return '❌ JSON inválido. Use o formato exportado por /export.';
                    }
                }
                return '';
            }

            case '/stats': {
                if (!this._usageTracker) return 'UsageTracker não disponível';
                return this._usageTracker.formatStats();
            }

            default:
                return '';
        }
    }
}
