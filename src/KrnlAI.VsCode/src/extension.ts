import { spawn } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { KernelClient } from './api/client';
import { ChatPanel } from './panels/chatPanel';
import { DashboardPanel } from './panels/dashboardPanel';
import { PoliciesPanel } from './panels/policiesPanel';
import { EpisodesPanel } from './panels/episodesPanel';
import { MemoryPanel } from './panels/memoryPanel';
import { SettingsPanel } from './panels/settingsPanel';
import { ChatViewProvider } from './chat/ChatViewProvider';
import { EditorContextProvider } from './codingAgent/EditorContextProvider';
import { SlashCommandManager } from './codingAgent/SlashCommandManager';
import { CodeLensProvider } from './codingAgent/CodeLensProvider';
import { CodeActionProvider } from './codingAgent/CodeActionProvider';
import { ApprovalManager, ApprovalMode } from './codingAgent/ApprovalManager';
import { CodingHoverProvider } from './codingAgent/CodingHoverProvider';
import { InlineCompletionProvider } from './codingAgent/InlineCompletionProvider';
import { ApplyEditManager, FileChange } from './codingAgent/ApplyEditManager';
import { TerminalManager } from './codingAgent/TerminalManager';
import { GitManager } from './codingAgent/GitManager';
import { AgenticLoopManager } from './codingAgent/AgenticLoopManager';
import { SessionManager } from './services/SessionManager';
import { UsageTracker } from './services/UsageTracker';
import { registerKernelChatParticipant } from './chat/KernelChatParticipant';

let sidecarProcess: any = undefined;
let statusBarItem: vscode.StatusBarItem;
let codingAgentDisposables: vscode.Disposable[] = [];
let slashMgr: SlashCommandManager | undefined;

export function activate(context: vscode.ExtensionContext) {
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.text = "$(hubot) Krnl-AI";
    statusBarItem.command = "krnlai.chat";
    statusBarItem.tooltip = "Clique para abrir o Chat";
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

    const client = new KernelClient();
    async function updateHealth() {
        const status = await client.getStatusMessage();
        statusBarItem.text = `$(hubot) ${status}`;
    }
    updateHealth();
    const healthTimer = setInterval(updateHealth, 30000);
    context.subscriptions.push(new vscode.Disposable(() => clearInterval(healthTimer)));

    context.subscriptions.push(new vscode.Disposable(() => {
        if (sidecarProcess) { sidecarProcess.kill(); sidecarProcess = undefined; }
    }));

    // Sidecar
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.start', async () => {
        if (sidecarProcess) { vscode.window.showInformationMessage('Sidecar já está rodando'); return; }

        // Sidecar path: env var > workspace config > project default
        let csprojPath = process.env.KRNL_SIDECAR_PATH || vscode.workspace.getConfiguration('krnlai').get<string>('sidecarPath', '');
        if (!csprojPath) {
            const projectDir = vscode.workspace.workspaceFolders?.[0]?.uri?.fsPath;
            if (!projectDir) {
                vscode.window.showErrorMessage('Configure "krnlai.sidecarPath" ou abra uma pasta do projeto Krnl-AI');
                return;
            }
            csprojPath = path.join(projectDir, 'src', 'KrnlAI.Sidecar', 'KrnlAI.Sidecar.csproj');
        }
        if (!fs.existsSync(csprojPath)) {
            vscode.window.showErrorMessage(`Sidecar não encontrado em: ${csprojPath}. Configure "krnlai.sidecarPath" ou a env var KRNL_SIDECAR_PATH`);
            return;
        }
        const transport = vscode.workspace.getConfiguration('krnlai').get<string>('sidecarTransport', 'http');
        const isStdio = transport === 'stdio';
        const args = isStdio ? ['run', '--project', csprojPath, '--', '--stdio'] : ['run', '--project', csprojPath];
        vscode.window.showInformationMessage(`Iniciando Krnl-AI Sidecar (${transport})...`);
        try {
            sidecarProcess = spawn('dotnet', args, {
                cwd: path.dirname(csprojPath),
                stdio: isStdio ? ['pipe', 'pipe', 'pipe'] : 'pipe'
            });
            const sanitizeLog = (data: any): string => {
                const s = String(data);
                return s.replace(/((?:token|secret|password|key|authorization|api_key)\s*[:=]\s*['"]?)[^\s'"&]+/gi, '$1***');
            };
            if (isStdio) {
                // JSON-RPC over stdio: send request, read response
                let buffer = '';
                sidecarProcess.stdout.on('data', (d: any) => {
                    buffer += String(d);
                    console.log(`[sidecar-rpc] ${sanitizeLog(d)}`);
                });
                sidecarProcess.stderr.on('data', (d: any) => console.error(`[sidecar-rpc] ${sanitizeLog(d)}`));
                sidecarProcess.on('close', (code: number) => { console.log(`Sidecar stdio exited: ${code}`); sidecarProcess = undefined; });
                setTimeout(() => { vscode.window.showInformationMessage('Sidecar iniciado em modo stdio/RPC'); updateHealth(); }, 3000);
            } else {
                sidecarProcess.stdout.on('data', (d: any) => console.log(`[sidecar] ${sanitizeLog(d)}`));
                sidecarProcess.stderr.on('data', (d: any) => console.error(`[sidecar] ${sanitizeLog(d)}`));
                sidecarProcess.on('close', (code: number) => { console.log(`Sidecar exited: ${code}`); sidecarProcess = undefined; });
                setTimeout(() => { vscode.window.showInformationMessage('Sidecar iniciado na porta 5001'); updateHealth(); }, 5000);
            }
        } catch (ex: any) { vscode.window.showErrorMessage(`Erro: ${ex.message}`); }
    }));

    context.subscriptions.push(vscode.commands.registerCommand('krnlai.stop', () => {
        if (sidecarProcess) { sidecarProcess.kill(); sidecarProcess = undefined; vscode.window.showInformationMessage('Sidecar parado'); updateHealth(); }
    }));

    // Panels (existing)
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.chat', () => ChatPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.dashboard', () => DashboardPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.policies', () => PoliciesPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.episodes', () => EpisodesPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.memory', () => MemoryPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.kanban', () => KanbanPanel.createOrShow()));
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.settings', () => SettingsPanel.createOrShow()));

    // TreeView
    const treeProvider = new NavTreeProvider();
    vscode.window.registerTreeDataProvider('krnlai.nav', treeProvider);
    context.subscriptions.push(vscode.commands.registerCommand('krnlai.navigate', (id: string) => vscode.commands.executeCommand(`krnlai.${id}`)));

    // Coding Agent mode (feature flag: krnlai.codingAgent.enabled)
    const config = vscode.workspace.getConfiguration('krnlai');
    const codingAgentEnabled = config.get<boolean>('codingAgent.enabled', false);

    if (codingAgentEnabled) {
        registerCodingAgentFeatures(context, client);
    }

    // Watch for config changes to enable/disable coding agent dynamically
    context.subscriptions.push(vscode.workspace.onDidChangeConfiguration(e => {
        if (e.affectsConfiguration('krnlai.codingAgent.enabled')) {
            const enabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.enabled', false);
            if (enabled && codingAgentDisposables.length === 0) {
                registerCodingAgentFeatures(context, client);
            } else if (!enabled && codingAgentDisposables.length > 0) {
                unregisterCodingAgentFeatures();
            }
        }
    }));
}

function unregisterCodingAgentFeatures() {
    for (const d of codingAgentDisposables) d.dispose();
    codingAgentDisposables = [];
    slashMgr = undefined;
}

function registerCodingAgentFeatures(context: vscode.ExtensionContext, client: KernelClient) {
    if (codingAgentDisposables.length > 0) return;

    const ctxProvider = new EditorContextProvider();
    const approvalManager = new ApprovalManager();
    codingAgentDisposables.push({ dispose: () => approvalManager.dispose() });
    const terminalEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.terminal', false);
    const terminalManager = terminalEnabled ? new TerminalManager() : undefined;
    const gitEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.git', false);
    const gitManager = gitEnabled ? new GitManager() : undefined;
    const agenticLoopsEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.agenticLoops', false);
    const loopManager = agenticLoopsEnabled && terminalManager
        ? new AgenticLoopManager(client, undefined, terminalManager, gitManager, approvalManager.getMode() !== ApprovalMode.Chat ? approvalManager : undefined)
        : undefined;
    slashMgr = new SlashCommandManager(client, terminalManager, gitManager, loopManager);

    function pushSub(d: vscode.Disposable) {
        codingAgentDisposables.push(d);
        context.subscriptions.push(d);
    }

    async function withApproval(action: string, details: string[], fn: () => Promise<void>) {
        const decision = await approvalManager.requestApproval(action, details);
        if (decision === 'rejected') {
            vscode.window.showInformationMessage(`Ação rejeitada: ${action}`);
            return;
        }
        await fn();
    }

    async function runSlashCommand(cmd: string, content: string, lang?: string) {
        if (!content) {
            vscode.window.showInformationMessage('Abra um arquivo ou selecione código primeiro');
            return;
        }
        const shouldApprove = approvalManager.getMode() !== ApprovalMode.Chat;
        if (shouldApprove) {
            const decision = await approvalManager.requestApproval(
                `executar ${cmd}`,
                [`Comando: ${cmd}`, `Contexto: ${content.substring(0, 100)}...`]
            );
            if (decision === 'rejected') return;
        }
        const ctx = await ctxProvider.getFullContext();
        const result = await slashMgr!.execute(`${cmd} ${content}`, ctx);

        if ((cmd === '/fix' || cmd === '/refactor') && ctx.activeFile) {
            const editManager = new ApplyEditManager();
            const codeMatch = result.match(/```(?:\w+)?\n([\s\S]*?)```/);
            const newCode = codeMatch ? codeMatch[1].trim() : result.trim();
            if (newCode && newCode !== content.trim()) {
                await editManager.applyWithDiff(
                    {
                        filePath: ctx.activeFile,
                        originalContent: content,
                        newContent: newCode,
                        label: cmd === '/fix' ? 'Correção' : 'Refatoração'
                    },
                    shouldApprove ? approvalManager : undefined
                );
                return;
            }
        }
        const doc = await vscode.workspace.openTextDocument({ content: result, language: lang || 'markdown' });
        vscode.window.showTextDocument(doc, { preview: true, viewColumn: vscode.ViewColumn.Beside });
    }

    const sessionManager = new SessionManager(context);
    const usageTracker = new UsageTracker(context);
    pushSub(vscode.commands.registerCommand('krnlai.coding.chat', () => ChatViewProvider.createOrShow(client, approvalManager, sessionManager, usageTracker)));

    const chatParticipantEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.chatParticipant', false);
    if (chatParticipantEnabled) {
        pushSub(registerKernelChatParticipant(context, client, approvalManager, sessionManager));
    }

    pushSub(vscode.commands.registerCommand('krnlai.coding.explain', () =>
        runSlashCommand('/explain', ctxProvider.getSelection() || ctxProvider.getActiveEditorContent()?.substring(0, 1000) || '')));
    pushSub(vscode.commands.registerCommand('krnlai.coding.fix', () =>
        runSlashCommand('/fix', ctxProvider.getSelection() || ctxProvider.getActiveEditorContent()?.substring(0, 1000) || '')));
    pushSub(vscode.commands.registerCommand('krnlai.coding.test', () => {
        const ctx = ctxProvider;
        return runSlashCommand('/test', ctx.getSelection() || ctx.getActiveEditorContent()?.substring(0, 1000) || '', ctx.getCurrentLanguage());
    }));
    pushSub(vscode.commands.registerCommand('krnlai.coding.refactor', () => {
        const ctx = ctxProvider;
        return runSlashCommand('/refactor', ctx.getSelection() || ctx.getActiveEditorContent()?.substring(0, 1000) || '', ctx.getCurrentLanguage());
    }));
    pushSub(vscode.commands.registerCommand('krnlai.coding.review', () => {
        const ctx = ctxProvider;
        return runSlashCommand('/review', ctx.getActiveEditorContent()?.substring(0, 2000) || '');
    }));
    pushSub(vscode.commands.registerCommand('krnlai.coding.run', async () => {
        const input = await vscode.window.showInputBox({ prompt: 'Digite um prompt personalizado...', placeHolder: 'Ex: refatore esta função para usar async/await' });
        if (input) {
            const content = ctxProvider.getSelection() || ctxProvider.getActiveEditorContent() || '';
            await runSlashCommand(input, content);
        }
    }));

    pushSub(vscode.languages.registerCodeLensProvider({ scheme: 'file' }, new CodeLensProvider()));

    pushSub(vscode.languages.registerCodeActionsProvider({ scheme: 'file' }, new CodeActionProvider()));

    pushSub(vscode.commands.registerCommand('krnlai.coding.setMode', (mode: string) => {
        const m = mode as ApprovalMode;
        if (Object.values(ApprovalMode).includes(m)) {
            approvalManager.setMode(m);
            vscode.window.showInformationMessage(`Modo de aprovação: ${m}`);
        }
    }));

    // Inline Completion (feature flag: krnlai.codingAgent.inlineCompletion)
    const inlineCompletionEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.inlineCompletion', false);
    if (inlineCompletionEnabled) {
        const inlineProvider = new InlineCompletionProvider(() => client.getBaseUrl());
        pushSub(vscode.languages.registerInlineCompletionItemProvider({ pattern: '**' }, inlineProvider));
        InlineCompletionProvider.registerAcceptNextWordCommand(context);
    }

    // Hover Provider (feature flag: krnlai.codingAgent.hover)
    const hoverEnabled = vscode.workspace.getConfiguration('krnlai').get<boolean>('codingAgent.hover', false);
    if (hoverEnabled) {
        const hoverProvider = new CodingHoverProvider(() => client.getBaseUrl());
        pushSub(vscode.languages.registerHoverProvider({ pattern: '**' }, hoverProvider));
    }
}

export function deactivate() {
    if (sidecarProcess) { sidecarProcess.kill(); sidecarProcess = undefined; }
    unregisterCodingAgentFeatures();
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
            new NavItem('📌 Kanban', 'kanban', 'Quadro Kanban'),
            new NavItem('⚙️ Configurações', 'settings', 'Configurações'),
        ];
    }
}

class NavItem extends vscode.TreeItem {
    constructor(label: string, public readonly id: string, tooltip: string) {
        super(label, vscode.TreeItemCollapsibleState.None);
        this.tooltip = tooltip;
        this.command = { command: 'krnlai.navigate', title: '', arguments: [id] };
    }
}
