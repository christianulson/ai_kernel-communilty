import * as vscode from 'vscode';
import { KernelClient } from '../api/client';

export class DiagnosticsTreeProvider implements vscode.TreeDataProvider<DiagnosticsNode> {
    private _onDidChangeTreeData = new vscode.EventEmitter<DiagnosticsNode | undefined>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private _data: DiagnosticsNode[] = [{ label: 'Status', description: 'Checking...', contextValue: 'loading' }];

    constructor(private _client: KernelClient) {
        this.refresh();
    }

    refresh(): void {
        this._client.health().then(health => {
            if (!health) {
                this._data = [{ label: 'API Status', description: '❌ Unreachable', contextValue: 'error' }];
            } else {
                this._data = [
                    { label: 'API Status', description: health.status === 'ok' ? '✅ Online' : '❌ Offline', contextValue: health.status },
                    { label: 'Version', description: `v${health.version}`, contextValue: 'version' },
                    { label: 'Sidecar', description: '—', contextValue: 'sidecar' },
                ];
            }
            this._onDidChangeTreeData.fire(undefined);
        }).catch(() => {
            this._data = [{ label: 'API Status', description: '❌ Unreachable', contextValue: 'error' }];
            this._onDidChangeTreeData.fire(undefined);
        });
    }

    getTreeItem(element: DiagnosticsNode): vscode.TreeItem {
        const item = new vscode.TreeItem(element.label);
        item.description = element.description;
        item.tooltip = `${element.label}: ${element.description}`;
        item.contextValue = element.contextValue;
        item.command = { command: 'krnlai.status.check', title: 'Refresh' };
        return item;
    }

    getChildren(_element?: DiagnosticsNode): DiagnosticsNode[] {
        return _element ? [] : this._data;
    }
}

interface DiagnosticsNode {
    label: string;
    description: string;
    contextValue: string;
}
