import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class BacklogPanel {
    public static currentPanel: BacklogPanel | undefined;
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
        this._panel.webview.onDidReceiveMessage((msg) => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (BacklogPanel.currentPanel) {
            BacklogPanel.currentPanel._panel.reveal();
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            'krnlai.backlog',
            'Krnl-AI - Backlog',
            vscode.ViewColumn.Beside,
            { enableScripts: true, retainContextWhenHidden: true },
        );
        BacklogPanel.currentPanel = new BacklogPanel(panel);
    }

    private async _handle(msg: any) {
        switch (msg.type) {
            case 'load':
            case 'refresh':
                const items = await this._client.getBacklogItems();
                this._panel.webview.postMessage({ type: 'items', items: items ?? [] });
                break;
            case 'create':
                const created = await this._client.createBacklogItem({
                    title: msg.title,
                    description: msg.description,
                    priority: msg.priority,
                    dependencies: msg.dependencies,
                    tags: msg.tags,
                });
                this._panel.webview.postMessage({
                    type: 'createResult',
                    item: created,
                    error: created ? undefined : 'Failed to create item',
                });
                if (created) {
                    const all = await this._client.getBacklogItems();
                    this._panel.webview.postMessage({ type: 'items', items: all ?? [] });
                }
                break;
            case 'updateStatus':
                const updated = await this._client.updateBacklogStatus(msg.id, msg.status);
                this._panel.webview.postMessage({
                    type: 'updateResult',
                    id: msg.id,
                    item: updated,
                    error: updated ? undefined : 'Failed to update',
                });
                if (updated) {
                    const all = await this._client.getBacklogItems();
                    this._panel.webview.postMessage({ type: 'items', items: all ?? [] });
                }
                break;
            case 'delete':
                const ok = await this._client.deleteBacklogItem(msg.id);
                this._panel.webview.postMessage({ type: 'deleteResult', id: msg.id, ok });
                if (ok) {
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
<title>Backlog</title>
<style nonce="${nonce}">
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:var(--vscode-font-family);background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:16px}
.header{display:flex;justify-content:space-between;align-items:center;margin-bottom:16px}
.header h2{margin:0;font-weight:600}.header button{background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;padding:6px 12px;border-radius:4px;cursor:pointer}
.columns{display:flex;gap:12px;overflow-x:auto;padding-bottom:16px}
.column{min-width:220px;flex:1;background:var(--vscode-sideBar-background);border-radius:8px;padding:12px}
.column h3{margin:0 0 8px;font-size:13px;text-transform:uppercase;opacity:.7;display:flex;justify-content:space-between}
.column h3 .count{font-size:11px;opacity:.5}
.card{background:var(--vscode-editor-background);border-radius:6px;padding:10px 12px;margin:0 0 8px;border:1px solid var(--vscode-widget-border);cursor:pointer;position:relative}
.card:hover{border-color:var(--vscode-focusBorder)}
.card .title{font-size:13px;font-weight:500;margin:0 0 4px}
.card .meta{font-size:11px;opacity:.6;display:flex;gap:8px;flex-wrap:wrap}
.card .priority{display:inline-block;padding:1px 6px;border-radius:3px;font-size:10px;font-weight:600}
.priority-Critical{background:#f44;color:#fff}.priority-High{background:#f90;color:#fff}
.priority-Medium{background:#09f;color:#fff}.priority-Low{background:#999;color:#fff}
.card .deps{font-size:10px;opacity:.5;margin-top:4px}.card .deps.blocked{color:var(--vscode-errorForeground);opacity:1}
.modal-overlay{display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,.5);z-index:1000;justify-content:center;align-items:center}
.modal-overlay.open{display:flex}
.modal{background:var(--vscode-editor-background);border:1px solid var(--vscode-widget-border);border-radius:8px;padding:20px;width:400px;max-width:90%}
.modal h3{margin:0 0 12px}.modal label{display:block;margin:8px 0 4px;font-size:12px;opacity:.7}
.modal input,.modal select,.modal textarea{width:100%;padding:6px 8px;background:var(--vscode-input-background);color:var(--vscode-input-foreground);border:1px solid var(--vscode-input-border);border-radius:4px;font-family:var(--vscode-font-family);font-size:13px;margin-bottom:8px}
.modal textarea{min-height:60px;resize:vertical}
.modal .actions{display:flex;gap:8px;justify-content:flex-end;margin-top:12px}
.modal .actions button{padding:6px 16px;border-radius:4px;border:none;cursor:pointer}
.modal .actions .primary{background:var(--vscode-button-background);color:var(--vscode-button-foreground)}
.modal .actions .secondary{background:var(--vscode-button-secondaryBackground);color:var(--vscode-button-secondaryForeground)}
.toast{position:fixed;bottom:16px;right:16px;padding:8px 16px;border-radius:4px;font-size:12px;z-index:2000;display:none}
.toast.error{background:var(--vscode-inputValidation-errorBackground);border:1px solid var(--vscode-inputValidation-errorBorder);display:block}
.toast.success{background:var(--vscode-inputValidation-infoBackground);border:1px solid var(--vscode-inputValidation-infoBorder);display:block}
.filter-bar{display:flex;gap:8px;margin-bottom:12px;align-items:center}
.filter-bar label{font-size:12px;opacity:.7}.filter-bar select{padding:4px 8px;background:var(--vscode-dropdown-background);color:var(--vscode-dropdown-foreground);border:1px solid var(--vscode-dropdown-border);border-radius:4px;font-size:12px}
</style></head><body>
<div class="header"><h2>📋 Backlog</h2><button onclick="showCreate()">+ Novo Item</button></div>
<div class="filter-bar"><label>Filtrar:</label><select id="statusFilter" onchange="applyFilter()">
<option value="">Todos</option><option value="Pending">Pendente</option><option value="InProgress">Em Progresso</option>
<option value="Review">Revisão</option><option value="Done">Concluído</option><option value="Cancelled">Cancelado</option>
</select></div>
<div class="columns" id="columns"></div>
<div class="modal-overlay" id="modal"><div class="modal">
<h3 id="modalTitle">Novo Item</h3>
<label>Título</label><input id="fieldTitle" placeholder="Título do item">
<label>Descrição</label><textarea id="fieldDesc" placeholder="Descrição"></textarea>
<label>Prioridade</label><select id="fieldPriority"><option value="Low">Baixa</option><option value="Medium" selected>Média</option><option value="High">Alta</option><option value="Critical">Crítica</option></select>
<label>Dependências (IDs separados por vírgula)</label><input id="fieldDeps" placeholder="B-001, B-002">
<label>Tags (separadas por vírgula)</label><input id="fieldTags" placeholder="frontend, bug">
<div class="actions"><button class="secondary" onclick="hideModal()">Cancelar</button><button class="primary" id="modalSave" onclick="saveItem()">Criar</button></div>
</div></div>
<div class="toast" id="toast"></div>
<script nonce="${nonce}">
(function(){const vscode=acquireVsCodeApi();let items=[];
function statusLabel(s){return{Pending:'Pendente',InProgress:'Em Progresso',Review:'Revisão',Done:'Concluído',Cancelled:'Cancelado'}[s]||s}
function render(filter){const cols=['Pending','InProgress','Review','Done','Cancelled'];const container=document.getElementById('columns');container.innerHTML='';
cols.forEach(status=>{const colItems=items.filter(i=>i.status===status&&(!filter||i.status===filter));
const col=document.createElement('div');col.className='column';
const h3=document.createElement('h3');h3.innerHTML=statusLabel(status)+' <span class="count">'+colItems.length+'</span>';col.appendChild(h3);
colItems.forEach(item=>{const card=document.createElement('div');card.className='card';
card.innerHTML='<div class="title">'+escapeHtml(item.title)+'</div><div class="meta"><span class="priority priority-'+item.priority+'">'+item.priority+'</span><span>'+item.id+'</span></div>';
if(item.dependencies&&item.dependencies.length){card.innerHTML+='<div class="deps">⬡ '+item.dependencies.join(', ')+'</div>'}
card.onclick=()=>showEdit(item);col.appendChild(card);});container.appendChild(col);});
const toast=document.getElementById('toast');toast.className='';toast.style.display='none';}
function applyFilter(){const filter=document.getElementById('statusFilter').value;render(filter);}
function escapeHtml(s){const d=document.createElement('div');d.textContent=s||'';return d.innerHTML;}
function showCreate(){document.getElementById('modalTitle').textContent='Novo Item';
document.getElementById('fieldTitle').value='';document.getElementById('fieldDesc').value='';
document.getElementById('fieldPriority').value='Medium';document.getElementById('fieldDeps').value='';
document.getElementById('fieldTags').value='';document.getElementById('modalSave').textContent='Criar';
document.getElementById('modalSave').onclick=saveItem;document.getElementById('modal').className='modal-overlay open';}
function showEdit(item){document.getElementById('modalTitle').textContent='Editar: '+item.id;
document.getElementById('fieldTitle').value=item.title;document.getElementById('fieldDesc').value=item.description;
document.getElementById('fieldPriority').value=item.priority;
document.getElementById('fieldDeps').value=(item.dependencies||[]).join(', ');
document.getElementById('fieldTags').value=(item.tags||[]).join(', ');
document.getElementById('modalSave').textContent='Salvar';
document.getElementById('modalSave').onclick=()=>{updateStatus(item);};document.getElementById('modal').className='modal-overlay open';}
function hideModal(){document.getElementById('modal').className='modal-overlay';}
function saveItem(){const title=document.getElementById('fieldTitle').value;if(!title.trim())return;
const deps=document.getElementById('fieldDeps').value.split(',').map(s=>s.trim()).filter(Boolean);
const tags=document.getElementById('fieldTags').value.split(',').map(s=>s.trim()).filter(Boolean);
vscode.postMessage({type:'create',title,description:document.getElementById('fieldDesc').value,
priority:document.getElementById('fieldPriority').value,dependencies:deps,tags:tags});hideModal();}
function updateStatus(item){const s=document.getElementById('fieldPriority').value;
if(s!==item.priority){const deps=document.getElementById('fieldDeps').value.split(',').map(x=>x.trim()).filter(Boolean);
const tags=document.getElementById('fieldTags').value.split(',').map(x=>x.trim()).filter(Boolean);
vscode.postMessage({type:'delete',id:item.id});
vscode.postMessage({type:'create',title:item.title,description:document.getElementById('fieldDesc').value,priority:s,dependencies:deps,tags:tags});}
hideModal();}
window.addEventListener('message',e=>{const msg=e.data;
if(msg.type==='items'){items=msg.items;applyFilter();}
else if(msg.type==='createResult'&&msg.error){showToast(msg.error,'error');}
else if(msg.type==='updateResult'&&msg.error){showToast(msg.error,'error');}
else if(msg.type==='deleteResult'&&!msg.ok){showToast('Erro ao excluir','error');}});
function showToast(msg,type){const t=document.getElementById('toast');t.textContent=msg;t.className='toast '+type;
setTimeout(()=>{t.className='';t.style.display='none';},3000);}
vscode.postMessage({type:'load'});})();
</script></body></html>`;
    }

    public dispose() {
        BacklogPanel.currentPanel = undefined;
        this._panel.dispose();
        while (this._disposables.length) {
            const d = this._disposables.pop();
            d?.dispose();
        }
    }
}
