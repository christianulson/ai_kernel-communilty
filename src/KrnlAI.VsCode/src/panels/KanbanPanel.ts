import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class KanbanPanel {
    public static currentPanel: KanbanPanel | undefined;
    public static readonly viewType = 'krnlai.kanban';
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private readonly _nonce: string;
    private readonly _disposables: vscode.Disposable[] = [];

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._client = new KernelClient();
        this._nonce = Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => this._handle(msg), null, this._disposables);
    }

    public static createOrShow() {
        const column = vscode.window.activeTextEditor ? vscode.window.activeTextEditor.viewColumn : undefined;
        if (KanbanPanel.currentPanel) {
            KanbanPanel.currentPanel._panel.reveal(column);
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            KanbanPanel.viewType, 'Kanban',
            column || vscode.ViewColumn.One,
            { enableScripts: true, retainContextWhenHidden: true }
        );
        KanbanPanel.currentPanel = new KanbanPanel(panel);
    }

    private async _handle(msg: any) {
        switch (msg.type) {
            case 'load':
                const items = await this._client.getBacklogItems();
                this._panel.webview.postMessage({ type: 'items', items: items ?? [] });
                break;
            case 'move':
                const updated = await this._client.updateBacklogStatus(msg.id, msg.status);
                if (updated) {
                    const all = await this._client.getBacklogItems();
                    this._panel.webview.postMessage({ type: 'items', items: all ?? [] });
                }
                break;
            case 'create':
                const created = await this._client.createBacklogItem({
                    title: msg.title, description: msg.description, priority: msg.priority
                });
                if (created) {
                    const all = await this._client.getBacklogItems();
                    this._panel.webview.postMessage({ type: 'items', items: all ?? [] });
                }
                break;
        }
    }

    private _getHtml(): string {
        const nonce = this._nonce;
        return `<!DOCTYPE html>
<html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>Kanban</title>
<style nonce="${nonce}">
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:var(--vscode-font-family);background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:16px}
.header{display:flex;justify-content:space-between;align-items:center;margin-bottom:16px}
.header h2{margin:0;font-weight:600}
.header button{background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;padding:6px 12px;border-radius:4px;cursor:pointer}
.columns{display:flex;gap:12px;overflow-x:auto;padding-bottom:16px;min-height:200px}
.column{min-width:220px;flex:1;background:var(--vscode-sideBar-background);border-radius:8px;padding:12px}
.column h3{margin:0 0 8px;font-size:13px;text-transform:uppercase;opacity:.7;display:flex;justify-content:space-between}
.column h3 .count{font-size:11px;opacity:.5}
.card{background:var(--vscode-editor-background);border-radius:6px;padding:10px 12px;margin:0 0 8px;border:1px solid var(--vscode-widget-border);cursor:pointer}
.card:hover{border-color:var(--vscode-focusBorder)}
.card .title{font-size:13px;font-weight:500;margin:0 0 4px}
.card .meta{font-size:11px;opacity:.6;display:flex;gap:8px;flex-wrap:wrap}
.card .priority{display:inline-block;padding:1px 6px;border-radius:3px;font-size:10px;font-weight:600}
.priority-Critical{background:#f44;color:#fff}.priority-High{background:#f90;color:#fff}
.priority-Medium{background:#09f;color:#fff}.priority-Low{background:#999;color:#fff}
.move-buttons{display:flex;gap:4px;margin-top:6px}
.move-buttons button{font-size:10px;padding:2px 6px;background:var(--vscode-button-secondaryBackground);color:var(--vscode-button-secondaryForeground);border:none;border-radius:3px;cursor:pointer}
.modal-overlay{display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,.5);z-index:1000;justify-content:center;align-items:center}
.modal-overlay.open{display:flex}
.modal{background:var(--vscode-editor-background);border:1px solid var(--vscode-widget-border);border-radius:8px;padding:20px;width:380px;max-width:90%}
.modal h3{margin:0 0 12px}.modal label{display:block;margin:8px 0 4px;font-size:12px;opacity:.7}
.modal input,.modal select,.modal textarea{width:100%;padding:6px 8px;background:var(--vscode-input-background);color:var(--vscode-input-foreground);border:1px solid var(--vscode-input-border);border-radius:4px;font-family:var(--vscode-font-family);font-size:13px;margin-bottom:8px}
.modal textarea{min-height:50px;resize:vertical}
.modal .actions{display:flex;gap:8px;justify-content:flex-end;margin-top:12px}
.modal .actions button{padding:6px 16px;border-radius:4px;border:none;cursor:pointer}
.modal .actions .primary{background:var(--vscode-button-background);color:var(--vscode-button-foreground)}
.modal .actions .secondary{background:var(--vscode-button-secondaryBackground);color:var(--vscode-button-secondaryForeground)}
</style></head><body>
<div class="header"><h2>📌 Kanban Board</h2><button onclick="showCreate()">+ Novo Card</button></div>
<div class="columns" id="columns"></div>
<div class="modal-overlay" id="modal"><div class="modal">
<h3 id="modalTitle">Novo Card</h3>
<label>Título</label><input id="fieldTitle" placeholder="Título do card">
<label>Descrição</label><textarea id="fieldDesc" placeholder="Descrição"></textarea>
<label>Prioridade</label><select id="fieldPriority"><option value="Low">Baixa</option><option value="Medium" selected>Média</option><option value="High">Alta</option><option value="Critical">Crítica</option></select>
<div class="actions"><button class="secondary" onclick="hideModal()">Cancelar</button><button class="primary" onclick="saveCard()">Criar</button></div>
</div></div>
<script nonce="${nonce}">
(function(){const vscode=acquireVsCodeApi();let items=[];
function statusLabel(s){return{Pending:'Pendente',InProgress:'Em Progresso',Review:'Revisão',Done:'Concluído',Cancelled:'Cancelado'}[s]||s}
function nextStatus(s){return{Pending:'InProgress',InProgress:'Review',Review:'Done',Done:'Cancelled',Cancelled:'Pending'}[s]||s}
function prevStatus(s){return{Cancelled:'Done',Done:'Review',Review:'InProgress',InProgress:'Pending',Pending:'Cancelled'}[s]||s}
function render(){const cols=['Pending','InProgress','Review','Done','Cancelled'];const container=document.getElementById('columns');container.innerHTML='';
cols.forEach(status=>{const colItems=items.filter(i=>i.status===status);
const col=document.createElement('div');col.className='column';
const h3=document.createElement('h3');h3.innerHTML=statusLabel(status)+' <span class="count">'+colItems.length+'</span>';col.appendChild(h3);
colItems.forEach(item=>{const card=document.createElement('div');card.className='card';
card.innerHTML='<div class="title">'+escapeHtml(item.title)+'</div><div class="meta"><span class="priority priority-'+item.priority+'">'+item.priority+'</span><span>'+item.id+'</span></div>';
card.innerHTML+='<div class="move-buttons"><button onclick="event.stopPropagation();moveItem(\''+item.id+'\',\''+prevStatus(item.status)+'\')">◀</button><button onclick="event.stopPropagation();moveItem(\''+item.id+'\',\''+nextStatus(item.status)+'\')">▶</button></div>';
col.appendChild(card);});container.appendChild(col);});}
function escapeHtml(s){const d=document.createElement('div');d.textContent=s||'';return d.innerHTML;}
function moveItem(id,status){vscode.postMessage({type:'move',id,status});}
function showCreate(){document.getElementById('fieldTitle').value='';document.getElementById('fieldDesc').value='';
document.getElementById('fieldPriority').value='Medium';document.getElementById('modal').className='modal-overlay open';}
function hideModal(){document.getElementById('modal').className='modal-overlay';}
function saveCard(){const title=document.getElementById('fieldTitle').value;if(!title.trim())return;
vscode.postMessage({type:'create',title,description:document.getElementById('fieldDesc').value,
priority:document.getElementById('fieldPriority').value});hideModal();}
window.addEventListener('message',e=>{const msg=e.data;if(msg.type==='items'){items=msg.items;render();}});
vscode.postMessage({type:'load'});})();
</script></body></html>`;
    }

    public dispose() {
        KanbanPanel.currentPanel = undefined;
        this._panel.dispose();
        while (this._disposables.length) {
            const d = this._disposables.pop();
            d?.dispose();
        }
    }
}
