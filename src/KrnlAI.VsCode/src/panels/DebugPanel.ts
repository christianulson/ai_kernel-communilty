import * as vscode from 'vscode';
import { OperationTracker, OperationCall, OperationState } from '../services/OperationTracker';

export class DebugPanel {
    public static currentPanel: DebugPanel | undefined;
    private _panel: vscode.WebviewPanel;
    private _tracker: OperationTracker;
    private _disposables: vscode.Disposable[] = [];
    private _updateInterval: NodeJS.Timeout | undefined;
    private _tab: 'trace' | 'stats' = 'trace';

    private constructor(panel: vscode.WebviewPanel, tracker: OperationTracker) {
        this._panel = panel;
        this._tracker = tracker;
        this._panel.webview.html = this._getHtml();
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        this._panel.webview.onDidReceiveMessage(msg => {
            if (msg.type === 'switchTab') this._tab = msg.tab;
        }, null, this._disposables);
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
        const history = this._tracker.history;
        const trace = this._tracker.formatTrace();
        const stats = this._computeStats(history);
        this._panel.webview.postMessage({ type: 'update', trace, stats, count: history.length });
    }

    private _computeStats(history: readonly OperationCall[]): string {
        if (history.length === 0) return 'No data.';
        const total = history.length;
        const success = history.filter(o => o.state === OperationState.Completed).length;
        const failed = history.filter(o => o.state === OperationState.Failed).length;
        const running = history.filter(o => o.state === OperationState.Running).length;

        const completed = history.filter(o => o.state === OperationState.Completed && o.elapsedMs > 0);
        const avgDuration = completed.length > 0
            ? (completed.reduce((s, o) => s + o.elapsedMs, 0) / completed.length).toFixed(0)
            : '—';

        // Group by name
        const byName = new Map<string, { count: number; failed: number; avg: number }>();
        for (const op of history) {
            const key = op.name.split('.')[0]; // group by prefix
            const e = byName.get(key) ?? { count: 0, failed: 0, avg: 0 };
            e.count++;
            if (op.state === OperationState.Failed) e.failed++;
            if (op.elapsedMs > 0) e.avg = e.avg === 0 ? op.elapsedMs : (e.avg + op.elapsedMs) / 2;
            byName.set(key, e);
        }

        const topSlowest = [...completed].sort((a, b) => b.elapsedMs - a.elapsedMs).slice(0, 5);

        let s = `**Total:** ${total} | ✅ ${success} | ❌ ${failed} | ⏳ ${running}\n`;
        s += `**Avg duration:** ${avgDuration}ms\n\n`;
        s += `### By Category\n`;
        for (const [name, st] of byName) {
            s += `- **${name}**: ${st.count} ops, ${st.failed} failed, avg ${st.avg.toFixed(0)}ms\n`;
        }
        if (topSlowest.length > 0) {
            s += `\n### 🐌 Slowest\n`;
            for (const op of topSlowest) {
                s += `- ${op.name}: ${op.elapsedMs}ms\n`;
            }
        }
        return s;
    }

    private _getHtml(): string {
        return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline';">
<title>Krnl-AI Debug</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; font-size: 13px; background: #1e1e1e; color: #d4d4d4; padding: 0; }
.tabs { display: flex; border-bottom: 1px solid #333; background: #252526; }
.tab { padding: 8px 16px; cursor: pointer; border: none; background: transparent; color: #888; font-size: 13px; }
.tab.active { color: #fff; border-bottom: 2px solid #569cd6; background: #2d2d2d; }
.content { padding: 12px; }
.trace { font-family: Consolas, 'Courier New', monospace; font-size: 12px; white-space: pre-wrap; line-height: 1.5; }
.stats { font-family: Consolas, 'Courier New', monospace; font-size: 12px; white-space: pre-wrap; line-height: 1.6; }
.empty { color: #888; font-style: italic; }
.count { color: #888; margin-bottom: 8px; }
</style>
</head>
<body>
<div class="tabs">
  <button class="tab active" data-tab="trace" onclick="switchTab('trace')">📋 Trace</button>
  <button class="tab" data-tab="stats" onclick="switchTab('stats')">📊 Stats</button>
</div>
<div class="content">
  <div class="count" id="count">0 operations</div>
  <div id="traceView" class="trace">Waiting for data...</div>
  <div id="statsView" class="stats" style="display:none">Loading...</div>
</div>
<script>
(function() {
    const vscode = acquireVsCodeApi();
    let currentTab = 'trace';

    window.switchTab = function(tab) {
        currentTab = tab;
        document.querySelectorAll('.tab').forEach(t => t.classList.toggle('active', t.dataset.tab === tab));
        document.getElementById('traceView').style.display = tab === 'trace' ? 'block' : 'none';
        document.getElementById('statsView').style.display = tab === 'stats' ? 'block' : 'none';
        vscode.postMessage({ type: 'switchTab', tab });
    };

    window.addEventListener('message', event => {
        const msg = event.data;
        if (msg.type === 'update') {
            document.getElementById('count').textContent = msg.count + ' operations';
            const traceEl = document.getElementById('traceView');
            traceEl.textContent = msg.trace || 'No operations tracked.';
            if (!msg.trace) traceEl.className = 'trace empty';

            const statsEl = document.getElementById('statsView');
            statsEl.textContent = msg.stats || 'No data.';
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
