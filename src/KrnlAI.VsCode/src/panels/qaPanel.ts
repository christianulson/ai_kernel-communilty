import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class QAPanel {
    public static currentPanel: QAPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _client: KernelClient;
    private readonly _nonce: string;
    private readonly _disposables: vscode.Disposable[] = [];
    private _pollTimer: ReturnType<typeof setInterval> | undefined;

    private constructor(panel: vscode.WebviewPanel) {
        this._panel = panel;
        this._client = new KernelClient();
        this._nonce = Math.random().toString(36).substring(2, 10);
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage((msg) => this._handle(msg), null, this._disposables);
    }

    static createOrShow() {
        if (QAPanel.currentPanel) {
            QAPanel.currentPanel._panel.reveal();
            return;
        }
        const panel = vscode.window.createWebviewPanel('krnlai.qa', 'Krnl-AI - QA Tests', vscode.ViewColumn.Beside, {
            enableScripts: true,
            retainContextWhenHidden: true,
        });
        QAPanel.currentPanel = new QAPanel(panel);
    }

    private async _handle(msg: any) {
        switch (msg.type) {
            case 'load':
                const runs = await this._client.getQARuns();
                this._panel.webview.postMessage({ type: 'runs', runs: runs ?? [] });
                break;
            case 'runTest':
                const run = await this._client.runQATest({
                    targetType: msg.targetType,
                    endpoint: msg.endpoint,
                    timeoutSeconds: msg.timeoutSeconds ?? 30,
                    testNames: msg.testNames,
                });
                this._panel.webview.postMessage({
                    type: 'runResult',
                    run,
                    error: run ? undefined : 'Failed to start test',
                });
                if (run) this._startPolling(run.id);
                break;
            case 'getEvidence':
                const evidence = await this._client.getQARunEvidence(msg.id);
                this._panel.webview.postMessage({ type: 'evidence', id: msg.id, evidence: evidence ?? [] });
                break;
            case 'refresh':
                const all = await this._client.getQARuns();
                this._panel.webview.postMessage({ type: 'runs', runs: all ?? [] });
                break;
        }
    }

    private async _startPolling(runId: string) {
        this._pollTimer = setInterval(async () => {
            const run = await this._client.getQARun(runId);
            if (run) {
                this._panel.webview.postMessage({ type: 'runUpdate', run });
                if (run.status === 'Passed' || run.status === 'Failed' || run.status === 'Error') {
                    this._stopPolling();
                }
            }
        }, 3000);
    }

    private _stopPolling() {
        if (this._pollTimer) {
            clearInterval(this._pollTimer);
            this._pollTimer = undefined;
        }
    }

    private _getHtml(): string {
        const nonce = this._nonce;
        return `<!DOCTYPE html>
<html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}';">
<title>QA Tests</title>
<style nonce="${nonce}">
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:var(--vscode-font-family);background:var(--vscode-editor-background);color:var(--vscode-editor-foreground);padding:16px}
.header{display:flex;justify-content:space-between;align-items:center;margin-bottom:16px}
.header h2{margin:0;font-weight:600}.header button{background:var(--vscode-button-background);color:var(--vscode-button-foreground);border:none;padding:6px 12px;border-radius:4px;cursor:pointer}
.run-list{display:flex;flex-direction:column;gap:8px}
.run-card{background:var(--vscode-editor-background);border:1px solid var(--vscode-widget-border);border-radius:6px;padding:12px;cursor:pointer}
.run-card:hover{border-color:var(--vscode-focusBorder)}
.run-card .header-row{display:flex;justify-content:space-between;align-items:center;margin-bottom:8px}
.run-card .id{font-size:11px;opacity:.6}.run-card .status{font-size:11px;font-weight:600;padding:2px 8px;border-radius:3px}
.status-Passed{background:#1a3;color:#fff}.status-Failed{background:#e33;color:#fff}.status-Running{background:#09f;color:#fff}.status-Error{background:#f90;color:#fff}.status-Pending{background:#999;color:#fff}
.run-card .steps{margin-top:8px}.step{display:flex;gap:8px;align-items:center;font-size:12px;padding:4px 0;border-top:1px solid var(--vscode-widget-border)}
.step:first-child{border-top:none}.step .dot{width:8px;height:8px;border-radius:50%;flex-shrink:0}
.dot-Passed{background:#1a3}.dot-Failed{background:#e33}.dot-Running{background:#09f}.dot-Error{background:#f90}.dot-Pending{background:#999}
.evidence-link{color:var(--vscode-textLink-foreground);cursor:pointer;font-size:11px;margin-left:auto;text-decoration:underline}
.modal-overlay{display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,.5);z-index:1000;justify-content:center;align-items:center}
.modal-overlay.open{display:flex}.modal{background:var(--vscode-editor-background);border:1px solid var(--vscode-widget-border);border-radius:8px;padding:20px;width:420px;max-width:90%}
.modal h3{margin:0 0 12px}.modal label{display:block;margin:8px 0 4px;font-size:12px;opacity:.7}
.modal input,.modal select{width:100%;padding:6px 8px;background:var(--vscode-input-background);color:var(--vscode-input-foreground);border:1px solid var(--vscode-input-border);border-radius:4px;font-size:13px;margin-bottom:8px}
.modal .actions{display:flex;gap:8px;justify-content:flex-end;margin-top:12px}
.modal .actions button{padding:6px 16px;border-radius:4px;border:none;cursor:pointer}
.modal .actions .primary{background:var(--vscode-button-background);color:var(--vscode-button-foreground)}
.modal .actions .secondary{background:var(--vscode-button-secondaryBackground);color:var(--vscode-button-secondaryForeground)}
.evidence-panel{background:var(--vscode-editor-background);border:1px solid var(--vscode-widget-border);border-radius:6px;padding:12px;margin-top:12px;display:none}
.evidence-panel.open{display:block}.evidence-panel h4{margin:0 0 8px;font-size:12px;opacity:.7}
.evidence-panel ul{list-style:none;padding:0}.evidence-panel li{padding:4px 0;font-size:12px;font-family:monospace}
.toast{position:fixed;bottom:16px;right:16px;padding:8px 16px;border-radius:4px;font-size:12px;z-index:2000;display:none}
.toast.error{background:var(--vscode-inputValidation-errorBackground);border:1px solid var(--vscode-inputValidation-errorBorder);display:block}
</style></head><body>
<div class="header"><h2>🧪 QA Tests</h2><button id="btn-new-test">+ Novo Teste</button></div>
<div class="run-list" id="runList"></div>
<div class="evidence-panel" id="evidencePanel"><h4>Evidências</h4><ul id="evidenceList"></ul></div>
<div class="modal-overlay" id="modal"><div class="modal">
<h3 id="modalTitle">Novo Teste</h3>
<label>Tipo</label><select id="fieldType"><option value="Smoke">Smoke</option><option value="Api">API</option><option value="Frontend">Frontend</option><option value="Performance">Performance</option><option value="Chaos">Chaos</option></select>
<label>Endpoint</label><input id="fieldEndpoint" placeholder="http://localhost:5000" value="http://localhost:5000">
<label>Timeout (s)</label><input id="fieldTimeout" type="number" value="30">
<div class="actions"><button class="secondary" id="btn-cancel">Cancelar</button><button class="primary" id="btn-start">Iniciar</button></div>
</div></div>
<div class="toast" id="toast"></div>
<script nonce="${nonce}">
(function(){const vscode=acquireVsCodeApi();let runs=[];
function render(){const container=document.getElementById('runList');container.innerHTML='';
runs.forEach(run=>{const card=document.createElement('div');card.className='run-card';
const header='<div class="header-row"><span class="id">'+run.id+' - '+run.targetType+'</span><span class="status status-'+run.status+'">'+run.status+'</span></div>';
let steps='';if(run.steps&&run.steps.length){steps='<div class="steps">';
run.steps.forEach(s=>{steps+='<div class="step"><span class="dot dot-'+s.status+'"></span><span>'+escapeHtml(s.name)+'</span><span style="opacity:.6;font-size:11px">'+escapeHtml(s.detail||'')+'</span>';});steps+='</div>';}
card.innerHTML=header+'<div style="font-size:12px;opacity:.7;margin-bottom:4px">'+escapeHtml(run.results||'')+'</div>'+steps;
if(run.evidence&&run.evidence.length){card.innerHTML+='<span class="evidence-link">📎 Evidências ('+run.evidence.length+')</span>';card.querySelector('.evidence-link')!.addEventListener('click',()=>showEvidence(run.id));}
card.onclick=()=>showEvidence(run.id);container.appendChild(card);});}
function escapeHtml(s){const d=document.createElement('div');d.textContent=s||'';return d.innerHTML;}
function showNewTest(){document.getElementById('modalTitle').textContent='Novo Teste';
document.getElementById('fieldType').value='Smoke';document.getElementById('fieldEndpoint').value='http://localhost:5000';
document.getElementById('fieldTimeout').value='30';document.getElementById('modal').className='modal-overlay open';}
function hideModal(){document.getElementById('modal').className='modal-overlay';}
function startTest(){const targetType=document.getElementById('fieldType').value;
const endpoint=document.getElementById('fieldEndpoint').value;
const timeoutSeconds=parseInt(document.getElementById('fieldTimeout').value)||30;
vscode.postMessage({type:'runTest',targetType,endpoint,timeoutSeconds});hideModal();}
function showEvidence(id){const panel=document.getElementById('evidencePanel');
const list=document.getElementById('evidenceList');list.innerHTML='Carregando...';
panel.className='evidence-panel open';vscode.postMessage({type:'getEvidence',id});}
window.addEventListener('message',e=>{const msg=e.data;
if(msg.type==='runs'){runs=msg.runs;render();}
else if(msg.type==='runResult'){if(msg.error){showToast(msg.error,'error');}else{runs=[msg.run,...runs];render();}}
else if(msg.type==='runUpdate'){const idx=runs.findIndex(r=>r.id===msg.run.id);if(idx>=0){runs[idx]=msg.run;render();}}
else if(msg.type==='evidence'){const list=document.getElementById('evidenceList');list.innerHTML='';
if(msg.evidence.length){msg.evidence.forEach(e=>{const li=document.createElement('li');li.textContent='📄 '+e;list.appendChild(li);});}else{list.innerHTML='<li style="opacity:.6">Nenhuma evidência</li>';}}});
function showToast(msg,type){const t=document.getElementById('toast');t.textContent=msg;t.className='toast '+type;setTimeout(()=>{t.className='';t.style.display='none';},3000);}
document.getElementById('btn-new-test')!.addEventListener('click',showNewTest);
document.getElementById('btn-cancel')!.addEventListener('click',hideModal);
document.getElementById('btn-start')!.addEventListener('click',startTest);
vscode.postMessage({type:'load'});})();
</script></body></html>`;
    }

    public dispose() {
        this._stopPolling();
        QAPanel.currentPanel = undefined;
        this._panel.dispose();
        while (this._disposables.length) {
            const d = this._disposables.pop();
            d?.dispose();
        }
    }
}
