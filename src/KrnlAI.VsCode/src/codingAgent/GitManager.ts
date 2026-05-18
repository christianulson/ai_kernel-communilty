import * as vscode from 'vscode';
import { spawn } from 'child_process';
import * as path from 'path';

const DANGEROUS_PATTERNS = [
    /^git\s+push\s+.*--force/i,
    /^git\s+reset\s+--hard/i,
    /^git\s+checkout\s+--hard/i,
    /^git\s+clean\s+-f[d]?/i,
    /^git\s+branch\s+-D/i,
    /^git\s+rebase\s+/i,
    /^git\s+merge\s+/i,
];

export interface GitResult {
    success: boolean;
    output: string;
    error?: string;
}

export interface BranchInfo {
    current: boolean;
    name: string;
    behind?: number;
    ahead?: number;
}

export class GitManager {
    private _workspaceRoot: string;

    constructor() {
        this._workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri?.fsPath || process.cwd();
    }

    private _runGit(args: string[], timeoutMs = 30_000): Promise<GitResult> {
        return new Promise(resolve => {
            const child = spawn('git', args, {
                cwd: this._workspaceRoot,
                timeout: timeoutMs,
                windowsHide: true
            });

            let output = '';
            let error = '';

            child.stdout?.on('data', (data: Buffer) => { output += data.toString(); });
            child.stderr?.on('data', (data: Buffer) => { error += data.toString(); });

            const timer = setTimeout(() => {
                child.kill();
                resolve({ success: false, output, error: error + '\n[TIMEOUT]' });
            }, timeoutMs);

            child.on('close', (code: number | null) => {
                clearTimeout(timer);
                resolve({
                    success: code === 0,
                    output: output.trim(),
                    error: error.trim() || undefined
                });
            });

            child.on('error', (err: Error) => {
                clearTimeout(timer);
                resolve({ success: false, output, error: err.message });
            });
        });
    }

    isDangerous(command: string): { dangerous: boolean; reason?: string } {
        const trimmed = command.trim();
        for (const pattern of DANGEROUS_PATTERNS) {
            if (pattern.test(trimmed)) {
                return {
                    dangerous: true,
                    reason: `Operação Git perigosa: ${pattern.source}. Requer aprovação manual.`
                };
            }
        }
        return { dangerous: false };
    }

    get dangerousPatterns(): readonly RegExp[] {
        return DANGEROUS_PATTERNS;
    }

    async commit(message: string): Promise<GitResult> {
        const addResult = await this._runGit(['add', '-A']);
        if (!addResult.success) return addResult;

        return this._runGit(['commit', '-m', message]);
    }

    async commitWithScope(scope: string, message: string): Promise<GitResult> {
        return this.commit(`${scope}: ${message}`);
    }

    async getDiff(staged = false): Promise<GitResult> {
        const args = staged ? ['diff', '--cached'] : ['diff'];
        return this._runGit(args);
    }

    async getBranches(): Promise<BranchInfo[]> {
        const result = await this._runGit(['branch', '-a']);
        if (!result.success) return [];

        return result.output
            .split('\n')
            .filter(b => b.trim())
            .map(b => ({
                current: b.startsWith('* '),
                name: b.replace('* ', '').trim().replace('remotes/', '')
            }));
    }

    async getCurrentBranch(): Promise<string> {
        const result = await this._runGit(['rev-parse', '--abbrev-ref', 'HEAD']);
        return result.success ? result.output : 'unknown';
    }

    async createBranch(name: string): Promise<GitResult> {
        return this._runGit(['checkout', '-b', name]);
    }

    async deleteBranch(name: string): Promise<GitResult> {
        return this._runGit(['branch', '-d', name]);
    }

    async getLog(maxCount = 10): Promise<GitResult> {
        return this._runGit(['log', `--max-count=${maxCount}`, '--oneline', '--decorate']);
    }

    async getStatus(): Promise<GitResult> {
        return this._runGit(['status', '--short']);
    }

    async getUncommittedChanges(): Promise<{ file: string; type: string }[]> {
        const result = await this._runGit(['status', '--porcelain']);
        if (!result.success) return [];

        return result.output
            .split('\n')
            .filter(l => l.trim())
            .map(l => ({
                type: l.substring(0, 2).trim(),
                file: l.substring(3).trim()
            }));
    }

    async push(remote = 'origin', branch?: string): Promise<GitResult> {
        const actualBranch = branch || await this.getCurrentBranch();
        return this._runGit(['push', remote, actualBranch]);
    }

    async pushWithForce(remote = 'origin', branch?: string): Promise<GitResult> {
        const actualBranch = branch || await this.getCurrentBranch();
        return this._runGit(['push', '--force', remote, actualBranch]);
    }

    async reviewPR(prNumber: number, repo?: string): Promise<string> {
        let logResult: GitResult;

        if (repo) {
            logResult = await this._runGit(['log', `origin/main..origin/${repo}/PR-${prNumber}`, '--oneline']);
        } else {
            logResult = await this._runGit(['log', `--max-count=${20}`, '--oneline']);
        }

        if (!logResult.success) return `Erro ao obter commits: ${logResult.error}`;

        const diffResult = await this._runGit(['diff', '--stat']);
        const branchInfo = await this.getCurrentBranch();

        return [
            `## Revisão de PR #${prNumber}`,
            ``,
            `**Branch atual:** ${branchInfo}`,
            ``,
            `**Commits:**`,
            logResult.output,
            ``,
            `**Mudanças:**`,
            diffResult.output || '(sem diff disponível)',
        ].join('\n');
    }
}
