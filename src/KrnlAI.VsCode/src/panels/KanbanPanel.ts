import * as vscode from 'vscode';

export class KanbanPanel {
    public static currentPanel: KanbanPanel | undefined;
    private static readonly viewType = 'krnlai.kanban';
    private readonly _panel: vscode.WebviewPanel;

    public static createOrShow() {
        const column = vscode.window.activeTextEditor ? vscode.window.activeTextEditor.viewColumn : undefined;
        if (KanbanPanel.currentPanel) { KanbanPanel.currentPanel._panel.reveal(column); return; }
        const panel = vscode.window.createWebviewPanel(KanbanPanel.viewType, 'Kanban', column || vscode.ViewColumn.One, { enableScripts: true });
        panel.webview.html = KanbanPanel._getHtml();
        panel.onDidDispose(() => { KanbanPanel.currentPanel = undefined; }, null, []);
        KanbanPanel.currentPanel = new KanbanPanel(panel);
    }

    private constructor(panel: vscode.WebviewPanel) { this._panel = panel; }

    private static _getHtml(): string {
        return `<!DOCTYPE html>
<html><head><meta charset="UTF-8"><style>
body { font-family: var(--vscode-font-family); background: var(--vscode-editor-background); color: var(--vscode-editor-foreground); padding: 16px; }
h2 { margin: 0 0 16px 0; font-weight: 600; }
.columns { display: flex; gap: 12px; overflow-x: auto; }
.column { min-width: 200px; background: var(--vscode-sideBar-background); border-radius: 8px; padding: 12px; }
.column h3 { margin: 0 0 8px 0; font-size: 13px; text-transform: uppercase; opacity: 0.7; }
.card { background: var(--vscode-editor-background); border-radius: 6px; padding: 8px 12px; margin: 0 0 8px 0; border: 1px solid var(--vscode-widget-border); cursor: pointer; }
.card:hover { border-color: var(--vscode-focusBorder); }
.card .title { font-size: 13px; font-weight: 500; margin: 0 0 4px 0; }
.card .meta { font-size: 11px; opacity: 0.6; }
</style></head><body>
<h2>📌 Kanban Board</h2>
<p style="opacity:0.6">Conecte-se ao backend para visualizar seus cards.</p>
</body></html>`;
    }
}
