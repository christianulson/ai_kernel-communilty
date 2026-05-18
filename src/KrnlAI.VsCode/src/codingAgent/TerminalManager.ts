import * as vscode from 'vscode';

const ALLOWED_PREFIXES = [
    'npm', 'npx', 'dotnet', 'git', 'node', 'python',
    'python3', 'pnpm', 'yarn', 'bun', 'cargo', 'make',
    'cmake', 'deno', 'go', 'rustc', 'tsc', 'eslint',
    'prettier', 'jest', 'vitest', 'mocha', 'ava',
    'nyc', 'docker', 'docker-compose', 'winget', 'choco',
    'pip', 'pip3', 'nuget', 'code', 'pwsh', 'bash',
    'sh', 'zsh', 'dir', 'ls', 'cat', 'type', 'echo',
    'mkdir', 'copy', 'cp', 'move', 'mv', 'del', 'rm',
    'cd', 'pwd', 'whoami', 'date', 'time',
];

const BLOCKED_PATTERNS = [
    /^(sudo|doas)\s+/i,
    /rm\s+-rf\s+(?:\/|\/[a-zA-Z])/,
    /(?:curl|wget)\s+.*\s*\|\s*(?:bash|sh|zsh)/,
    /format\s+[a-zA-Z]:\/\s*\/FS:\s*NTFS/i,
    /dd\s+if=.*of=\/dev\/sda/,
    /mkfs\./,
    /fdisk/,
    /powershell\s+-ExecutionPolicy\s+Bypass/i,
];

export interface CommandResult {
    stdout: string;
    stderr: string;
    exitCode: number | undefined;
    command: string;
}

export class TerminalManager {
    private _terminal: vscode.Terminal | undefined;
    private _executionCount = 0;

    isAllowed(command: string): { allowed: boolean; reason?: string } {
        const trimmed = command.trim();

        for (const pattern of BLOCKED_PATTERNS) {
            if (pattern.test(trimmed)) {
                return { allowed: false, reason: `Comando bloqueado por segurança: corresponde a padrão perigoso` };
            }
        }

        const firstToken = trimmed.split(/\s+/)[0]?.toLowerCase();
        if (!firstToken) return { allowed: false, reason: 'Comando vazio' };

        const matched = ALLOWED_PREFIXES.some(prefix =>
            firstToken === prefix || firstToken.startsWith(prefix + ':')
        );

        if (!matched) {
            return { allowed: false, reason: `Comando não está na allowlist: ${firstToken}` };
        }

        return { allowed: true };
    }

    get allowlist(): readonly string[] {
        return ALLOWED_PREFIXES;
    }

    async runCommand(
        command: string,
        cwd?: string,
        timeoutMs = 60_000
    ): Promise<CommandResult> {
        const check = this.isAllowed(command);
        if (!check.allowed) {
            return {
                stdout: '',
                stderr: check.reason || 'Comando não permitido',
                exitCode: 1,
                command
            };
        }

        const terminalName = `Krnl-AI #${++this._executionCount}`;
        this._terminal = vscode.window.createTerminal(terminalName);
        this._terminal.show(false);

        const workspacePath = cwd || vscode.workspace.workspaceFolders?.[0]?.uri?.fsPath;

        if (workspacePath) {
            this._terminal.sendText(`cd "${workspacePath}"`);
        }

        this._terminal.sendText(command);

        return {
            stdout: `Comando enviado: ${command}`,
            stderr: '',
            exitCode: undefined,
            command
        };
    }

    async runCommandWithOutput(
        command: string,
        cwd?: string,
        timeoutMs = 60_000
    ): Promise<CommandResult> {
        const check = this.isAllowed(command);
        if (!check.allowed) {
            return {
                stdout: '',
                stderr: check.reason || 'Comando não permitido',
                exitCode: 1,
                command
            };
        }

        const workspacePath = cwd || vscode.workspace.workspaceFolders?.[0]?.uri?.fsPath;
        const shellPath = process.env.SHELL || process.env.ComSpec || 'cmd';
        const shellArgs = process.env.SHELL ? ['-c', command] : ['/c', command];

        return new Promise<CommandResult>((resolve) => {
            const { spawn } = require('child_process');
            const child = spawn(shellPath, shellArgs, {
                cwd: workspacePath,
                timeout: timeoutMs,
                windowsHide: true
            });

            let stdout = '';
            let stderr = '';

            child.stdout?.on('data', (data: Buffer) => { stdout += data.toString(); });
            child.stderr?.on('data', (data: Buffer) => { stderr += data.toString(); });

            const timer = setTimeout(() => {
                child.kill();
                resolve({ stdout, stderr: stderr + '\n[TIMEOUT]', exitCode: null as any, command });
            }, timeoutMs);

            child.on('close', (code: number | null) => {
                clearTimeout(timer);
                resolve({ stdout, stderr, exitCode: code ?? undefined, command });
            });

            child.on('error', (err: Error) => {
                clearTimeout(timer);
                resolve({ stdout, stderr: err.message, exitCode: 1, command });
            });
        });
    }

    dispose(): void {
        if (this._terminal) {
            this._terminal.dispose();
            this._terminal = undefined;
        }
    }
}
