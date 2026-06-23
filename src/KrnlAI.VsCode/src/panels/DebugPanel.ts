import * as vscode from 'vscode';
import { OperationTracker } from '../services/OperationTracker';
import { DebugManager } from '../services/DebugManager';

export class DebugPanel {
    public static currentPanel: DebugPanel | undefined;
    private _panel: vscode.WebviewPanel;
    private _tracker: OperationTracker;
    private _disposables: vscode.Disposable[] = [];
    private _updateInterval: NodeJS.Timeout | undefined;

    private constructor(panel: vscode.WebviewPanel, tracker: OperationTracker) {
        this._panel = panel;
        this._tracker = tracker;
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._refresh();
        this._updateInterval = setInterval(() => this._refresh(), 2000);
    }

    static createOrShow(tracker?: OperationTracker) {
        if (DebugPanel.currentPanel) {
            DebugPanel.currentPanel._panel.reveal(vscode.ViewColumn.Beside);
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            'krnlai.debugTrace', 'Krnl-AI Debug Trace',
            vscode.ViewColumn.Beside,
            { enableScripts: true, retainContextWhenHidden: true }
        );
        DebugPanel.currentPanel = new DebugPanel(panel, tracker ?? new OperationTracker());
    }

    private _refresh(): void {
        const trace = this._tracker.formatTrace();
        this._panel.webview.postMessage({
            type: 'update',
            trace,
            count: this._tracker.history.length,
        });
    }

    private _getHtml(): string {
        return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline';">
<title>Krnl-AI Debug Trace</title>
<style>
body { font-family: Consolas, 'Courier New', monospace; font-size: 12px; padding: 8px; background: #1e1e1e; color: #d4d4d4; }
h2 { margin: 0 0 8px 0; font-size: 14px; color: #569cd6; }
.count { color: #888; margin-bottom: 8px; }
.trace { white-space: pre-wrap; line-height: 1.5; }
.empty { color: #888; font-style: italic; }
</style>
</head>
<body>
<h2>🧠 Debug Trace</h2>
<div class="count" id="count">0 operations</div>
<div class="trace" id="trace">Waiting for data...</div>
<script>
(function() {
    const vscode = acquireVsCodeApi();
    window.addEventListener('message', event => {
        const msg = event.data;
        if (msg.type === 'update') {
            document.getElementById('count').textContent = msg.count + ' operations';
            document.getElementById('trace').textContent = msg.trace || 'No operations tracked.';
        }
    });
}());
</script>
</body>
</html>`;
    }

    dispose(): void {
        DebugPanel.currentPanel = undefined;
        if (this._updateInterval) clearInterval(this._updateInterval);
        this._panel.dispose();
        for (const d of this._disposables) d.dispose();
        this._disposables = [];
    }
}
