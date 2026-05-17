import * as vscode from 'vscode';
import { KernelClient } from '../api/client';
import { EditorContext } from './EditorContextProvider';
import { ApplyEditManager, FileChange } from './ApplyEditManager';
import { TerminalManager } from './TerminalManager';
import { GitManager } from './GitManager';
import type { ApprovalDecision } from './ApprovalManager';

const MAX_ITERATIONS = 10;
const MAX_FILES_PER_CYCLE = 5;
const LOOP_TIMEOUT_MS = 120_000;

export interface LoopStep {
    type: 'read' | 'edit' | 'run' | 'search' | 'think' | 'complete' | 'error';
    description: string;
    file?: string;
    command?: string;
    content?: string;
    result?: string;
    success?: boolean;
}

export interface LoopResult {
    task: string;
    iterations: number;
    completed: boolean;
    steps: LoopStep[];
    changes: FileChange[];
    error?: string;
    durationMs: number;
}

interface ApprovalGate {
    requestApproval(action: string, details: string[]): Promise<ApprovalDecision>;
}

export class AgenticLoopManager {
    private _client: KernelClient;
    private _editManager: ApplyEditManager;
    private _terminalManager?: TerminalManager;
    private _gitManager?: GitManager;
    private _approvalGate?: ApprovalGate;

    constructor(
        client: KernelClient,
        editManager?: ApplyEditManager,
        terminalManager?: TerminalManager,
        gitManager?: GitManager,
        approvalGate?: ApprovalGate
    ) {
        this._client = client;
        this._editManager = editManager || new ApplyEditManager();
        this._terminalManager = terminalManager;
        this._gitManager = gitManager;
        this._approvalGate = approvalGate;
    }

    async executeTask(task: string, context: EditorContext): Promise<LoopResult> {
        const startTime = Date.now();
        const steps: LoopStep[] = [];
        const changes: FileChange[] = [];
        let iteration = 0;

        const addStep = (step: LoopStep) => {
            steps.push(step);
            return step;
        };

        let currentContext = { ...context };

        while (iteration < MAX_ITERATIONS) {
            if (Date.now() - startTime > LOOP_TIMEOUT_MS) {
                addStep({ type: 'error', description: 'Timeout do loop', success: false });
                break;
            }

            iteration++;

            const planResult = await this._planNextStep(task, currentContext, steps);
            addStep({ type: 'think', description: `Iteração ${iteration}: ${planResult}`, success: true });

            if (this._isComplete(planResult)) {
                addStep({ type: 'complete', description: 'Tarefa concluída', success: true });
                return {
                    task, iterations: iteration, completed: true, steps, changes,
                    durationMs: Date.now() - startTime
                };
            }

            const action = await this._parseAction(planResult);
            if (!action) {
                addStep({ type: 'error', description: 'Não foi possível interpretar o plano', success: false });
                break;
            }

            switch (action.type) {
                case 'read': {
                    const step = addStep({ type: 'read', description: `Lendo ${action.file}`, file: action.file });
                    try {
                        const content = await this._readFile(action.file!);
                        step.content = content;
                        step.success = true;
                        currentContext.content = content;
                        currentContext.activeFile = action.file;
                    } catch (err: any) {
                        step.success = false;
                        step.result = err.message;
                    }
                    break;
                }

                case 'edit': {
                    const step = addStep({
                        type: 'edit', description: `Editando ${action.file}`, file: action.file,
                        content: action.content
                    });

                    if (!currentContext.activeFile && currentContext.content && currentContext.activeFile !== action.file) {
                        try {
                            const existingContent = await this._readFile(action.file!);
                            currentContext.content = existingContent;
                            currentContext.activeFile = action.file;
                        } catch {
                            step.success = false;
                            step.result = 'Arquivo não encontrado';
                            continue;
                        }
                    }

                    const fileChange: FileChange = {
                        filePath: action.file!,
                        originalContent: currentContext.content || '',
                        newContent: action.content || '',
                        label: `Iteração ${iteration}`
                    };

                    const approved = await this._editManager.applyWithDiff(fileChange, this._approvalGate);
                    if (approved) {
                        changes.push(fileChange);
                        currentContext.content = action.content;
                        step.success = true;
                    } else {
                        step.success = false;
                        step.result = 'Edit rejeitada';
                    }
                    break;
                }

                case 'run': {
                    const step = addStep({
                        type: 'run', description: `Executando: ${action.command}`, command: action.command
                    });

                    if (!this._terminalManager) {
                        step.success = false;
                        step.result = 'TerminalManager não disponível';
                        break;
                    }

                    const check = this._terminalManager.isAllowed(action.command || '');
                    if (!check.allowed) {
                        if (this._approvalGate) {
                            const decision = await this._approvalGate.requestApproval(
                                `Executar comando: ${action.command}`,
                                [check.reason || 'Comando não está na allowlist']
                            );
                            if (decision === 'rejected') {
                                step.success = false;
                                step.result = 'Comando rejeitado';
                                break;
                            }
                        } else {
                            step.success = false;
                            step.result = check.reason || 'Comando não permitido';
                            break;
                        }
                    }

                    const result = await this._terminalManager.runCommandWithOutput(action.command || '');
                    step.result = result.stdout || result.stderr;
                    step.success = result.exitCode === 0;

                    if (result.exitCode !== 0) {
                        const fixStep = addStep({
                            type: 'think',
                            description: `Comando falhou (exit ${result.exitCode}). Planejando correção...`,
                            success: true,
                            result: result.stderr
                        });
                    }
                    break;
                }

                case 'search': {
                    const step = addStep({
                        type: 'search', description: `Buscando: ${action.query}`
                    });
                    try {
                        const files = await vscode.workspace.findFiles(
                            action.query || '**/*', undefined, 10
                        );
                        step.result = files.map(f => f.fsPath).join('\n');
                        step.success = true;
                    } catch (err: any) {
                        step.success = false;
                        step.result = err.message;
                    }
                    break;
                }
            }

            await this._updateDiagnostics(currentContext);
        }

        return {
            task, iterations: iteration, completed: false, steps, changes,
            error: `Máximo de ${MAX_ITERATIONS} iterações atingido`,
            durationMs: Date.now() - startTime
        };
    }

    private async _planNextStep(
        task: string,
        context: EditorContext,
        previousSteps: LoopStep[]
    ): Promise<string> {
        const recentSteps = previousSteps.slice(-5);
        const historyStr = recentSteps.map(s =>
            `[${s.type}] ${s.description}${s.success === false ? ' (FALHOU)' : ''}${s.result ? `\n  Resultado: ${s.result.substring(0, 200)}` : ''}`
        ).join('\n');

        const prompt = [
            `## Task`,
            task,
            ``,
            `## Current Context`,
            `Active file: ${context.activeFile || 'none'}`,
            `Language: ${context.language || 'unknown'}`,
            `Diagnostics: ${context.diagnostics.length} issues`,
            ``,
            `## Previous Steps`,
            historyStr || '(nenhum passo anterior)',
            ``,
            `## Available Actions`,
            `- READ <filepath> - Read a file to understand its contents`,
            `- EDIT <filepath> with content:\`\`\`\n<new content>\n\`\`\``,
            `- RUN <shell command> - Execute a terminal command`,
            `- SEARCH <glob pattern> - Search for files`,
            `- COMPLETE - Task is done, summarize what was accomplished`,
            ``,
            `## Instructions`,
            `Decide the NEXT action. Return exactly ONE action in this format:`,
            `ACTION: <READ|EDIT|RUN|SEARCH|COMPLETE>`,
            `DETAILS: <action details>`,
            `CONTENT: <only for EDIT actions: the complete new file content>`,
        ].join('\n');

        const response = await this._client.runAgent(prompt);
        return response.narration || 'COMPLETE';
    }

    private _isComplete(plan: string): boolean {
        return plan.includes('COMPLETE') || plan.includes('complete') || plan.trim() === '';
    }

    private _parseAction(plan: string): { type: 'read' | 'edit' | 'run' | 'search'; file?: string; command?: string; content?: string; query?: string } | null {
        const lines = plan.split('\n');
        let actionType = '';
        let details = '';
        let content = '';

        let inContent = false;
        const contentLines: string[] = [];

        for (const line of lines) {
            const actionMatch = line.match(/^ACTION:\s*(\w+)/i);
            if (actionMatch) {
                actionType = actionMatch[1].toUpperCase();
                continue;
            }

            const detailsMatch = line.match(/^DETAILS:\s*(.+)/i);
            if (detailsMatch) {
                details = detailsMatch[1].trim();
                continue;
            }

            const contentMatch = line.match(/^CONTENT:\s*/i);
            if (contentMatch) {
                inContent = true;
                continue;
            }

            if (line.trim().startsWith('```')) {
                inContent = !inContent;
                continue;
            }

            if (inContent) {
                contentLines.push(line);
            }
        }

        content = contentLines.join('\n').trim();

        switch (actionType) {
            case 'READ':
                return { type: 'read', file: details || undefined };
            case 'EDIT':
                return { type: 'edit', file: details || undefined, content: content || undefined };
            case 'RUN':
                return { type: 'run', command: details || undefined };
            case 'SEARCH':
                return { type: 'search', query: details || undefined };
            default:
                return null;
        }
    }

    private async _readFile(filePath: string): Promise<string> {
        if (!filePath) throw new Error('Caminho de arquivo vazio');

        const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri;
        let uri: vscode.Uri;

        if (filePath.startsWith('/') || filePath.match(/^[a-zA-Z]:\\/)) {
            uri = vscode.Uri.file(filePath);
        } else if (workspaceRoot) {
            uri = vscode.Uri.joinPath(workspaceRoot, filePath);
        } else {
            throw new Error(`Arquivo não encontrado: ${filePath}`);
        }

        const doc = await vscode.workspace.openTextDocument(uri);
        return doc.getText();
    }

    private async _updateDiagnostics(context: EditorContext): Promise<void> {
        const activeUri = vscode.window.activeTextEditor?.document.uri;
        if (!activeUri) return;

        const diags: { message: string; severity: string; source?: string }[] = [];
        for (const [uri, ds] of vscode.languages.getDiagnostics()) {
            if (uri.toString() !== activeUri.toString()) continue;
            for (const d of ds) {
                diags.push({
                    message: d.message,
                    severity: d.severity === vscode.DiagnosticSeverity.Error ? 'error' : 'warning',
                    source: d.source
                });
            }
        }
        context.diagnostics = diags;
    }

    formatResult(result: LoopResult): string {
        const lines: string[] = [
            `## Resultado do Loop`,
            ``,
            `**Tarefa:** ${result.task}`,
            `**Status:** ${result.completed ? '✅ Completo' : '❌ Incompleto'}`,
            `**Iterações:** ${result.iterations}`,
            `**Duração:** ${(result.durationMs / 1000).toFixed(1)}s`,
            `**Arquivos modificados:** ${result.changes.length}`,
            ``,
            `### Passos Executados`,
            ``,
        ];

        for (const step of result.steps) {
            const icon = step.success === false ? '❌' : step.success === true ? '✅' : '➡️';
            lines.push(`${icon} **${step.type}:** ${step.description}`);
            if (step.file) lines.push(`   Arquivo: \`${step.file}\``);
            if (step.command) lines.push(`   Comando: \`${step.command}\``);
            if (step.result) lines.push(`   Resultado: ${step.result.substring(0, 300)}`);
        }

        if (result.error) {
            lines.push(``, `### Erro`, ``, result.error);
        }

        return lines.join('\n');
    }
}
