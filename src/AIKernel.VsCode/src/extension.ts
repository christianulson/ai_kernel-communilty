import { spawn } from 'child_process';
import * as path from 'path';
import * as vscode from 'vscode';
import { KernelClient } from './api/client';
import { ChatPanel } from './panels/chatPanel';
import { DashboardPanel } from './panels/dashboardPanel';
import { PoliciesPanel } from './panels/policiesPanel';
import { EpisodesPanel } from './panels/episodesPanel';
import { MemoryPanel } from './panels/memoryPanel';
import { SettingsPanel } from './panels/settingsPanel';

let sidecarProcess: any = undefined;
let statusBarItem: vscode.StatusBarItem;

export function activate(context: vscode.ExtensionContext) {
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.text = "$(hubot) AI Kernel";
    statusBarItem.command = "aikernel.chat";
    statusBarItem.tooltip = "Clique para abrir o Chat";
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

    const client = new KernelClient();
    async function updateHealth() {
        const status = await client.getStatusMessage();
        statusBarItem.text = `$(hubot) ${status}`;
    }
    updateHealth();
    setInterval(updateHealth, 30000);

    // Sidecar
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.start', async () => {
        if (sidecarProcess) { vscode.window.showInformationMessage('Sidecar já está rodando'); return; }
        const projectDir = vscode.workspace.workspaceFolders?.[0]?.uri?.fsPath;
        if (!projectDir) {
            vscode.window.showErrorMessage('Abra uma pasta do projeto AI Kernel para iniciar o Sidecar');
            return;
        }
        const csprojPath = path.join(projectDir, 'src', 'AIKernel.Sidecar', 'AIKernel.Sidecar.csproj');
        const fs = require('fs');
        if (!fs.existsSync(csprojPath)) {
            vscode.window.showErrorMessage(`Sidecar não encontrado em: ${csprojPath}`);
            return;
        }
        vscode.window.showInformationMessage('Iniciando AI Kernel Sidecar...');
        try {
            sidecarProcess = spawn('dotnet', ['run', '--project', csprojPath], {
                cwd: path.dirname(csprojPath),
                stdio: 'pipe'
            });
            sidecarProcess.stdout.on('data', (d: any) => console.log(`[sidecar] ${d}`));
            sidecarProcess.stderr.on('data', (d: any) => console.error(`[sidecar] ${d}`));
            sidecarProcess.on('close', (code: number) => { console.log(`Sidecar exited: ${code}`); sidecarProcess = undefined; });
            setTimeout(() => { vscode.window.showInformationMessage('Sidecar iniciado na porta 5001'); updateHealth(); }, 5000);
        } catch (ex: any) { vscode.window.showErrorMessage(`Erro: ${ex.message}`); }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('aikernel.stop', () => {
        if (sidecarProcess) { sidecarProcess.kill(); sidecarProcess = undefined; vscode.window.showInformationMessage('Sidecar parado'); updateHealth(); }
    }));

    // Panels
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.chat', () => ChatPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.dashboard', () => DashboardPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.policies', () => PoliciesPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.episodes', () => EpisodesPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.memory', () => MemoryPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.settings', () => SettingsPanel.createOrShow()));

    // TreeView
    const treeProvider = new NavTreeProvider();
    vscode.window.registerTreeDataProvider('aikernel.nav', treeProvider);
    context.subscriptions.push(vscode.commands.registerCommand('aikernel.navigate', (id: string) => vscode.commands.executeCommand(`aikernel.${id}`)));
}

export function deactivate() {
    if (sidecarProcess) { sidecarProcess.kill(); sidecarProcess = undefined; }
}

class NavTreeProvider implements vscode.TreeDataProvider<NavItem> {
    private _onDidChangeTreeData = new vscode.EventEmitter<NavItem | undefined>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;
    getTreeItem(element: NavItem): vscode.TreeItem { return element; }
    getChildren(element?: NavItem): NavItem[] {
        if (element) return [];
        return [
            new NavItem('💬 Chat', 'chat', 'Chat com o agente'),
            new NavItem('📊 Dashboard', 'dashboard', 'Métricas e saúde'),
            new NavItem('📋 Políticas', 'policies', 'Políticas aprendidas'),
            new NavItem('📜 Episódios', 'episodes', 'Histórico de execuções'),
            new NavItem('🧠 Memória', 'memory', 'Busca semântica'),
            new NavItem('⚙️ Configurações', 'settings', 'Configurações'),
        ];
    }
}

class NavItem extends vscode.TreeItem {
    constructor(label: string, public readonly id: string, tooltip: string) {
        super(label, vscode.TreeItemCollapsibleState.None);
        this.tooltip = tooltip;
        this.command = { command: 'aikernel.navigate', title: '', arguments: [id] };
    }
}
