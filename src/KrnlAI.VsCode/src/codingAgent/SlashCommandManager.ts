import * as vscode from 'vscode';
import { EditorContext } from './EditorContextProvider';
import { KernelClient } from '../api/client';
import { TerminalManager } from './TerminalManager';
import { GitManager } from './GitManager';
import { AgenticLoopManager } from './AgenticLoopManager';

export type SlashCommandHandler = (args: string, context: EditorContext, client: KernelClient) => Promise<string>;

export interface SlashCommand {
    id: string;
    description: string;
    handler: SlashCommandHandler;
}

export class SlashCommandManager {
    private _commands = new Map<string, SlashCommand>();
    private _client: KernelClient;
    private _terminalManager?: TerminalManager;
    private _gitManager?: GitManager;
    private _loopManager?: AgenticLoopManager;

    constructor(client: KernelClient, terminalManager?: TerminalManager, gitManager?: GitManager, loopManager?: AgenticLoopManager) {
        this._client = client;
        this._terminalManager = terminalManager;
        this._gitManager = gitManager;
        this._loopManager = loopManager;
        this.registerDefaults();
    }

    private registerDefaults() {
        this.register({
            id: '/explain',
            description: 'Explica o código com contexto do editor',
            handler: async (args, ctx, client) => {
                const prompt = args || ctx.selection || ctx.content?.substring(0, 2000) || '';
                const response = await client.runAgent(`Explique este código:\n\`\`\`${ctx.language || ''}\n${prompt}\n\`\`\``);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/fix',
            description: 'Tenta corrigir código baseado em diagnostics',
            handler: async (args, ctx, client) => {
                const code = args || ctx.selection || ctx.content?.substring(0, 2000) || '';
                const diags = ctx.diagnostics.slice(0, 20).map(d => `[${d.severity}] ${d.message}`).join('\n');
                const prompt = `Corrija este código considerando os diagnósticos:\n${diags}\n\nCódigo:\n\`\`\`${ctx.language || ''}\n${code}\n\`\`\``;
                const response = await client.runAgent(prompt);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/test',
            description: 'Gera teste unitário para a seleção',
            handler: async (args, ctx, client) => {
                const code = args || ctx.selection || ctx.content?.substring(0, 2000) || '';
                const prompt = `Gere testes unitários para este código:\n\`\`\`${ctx.language || ''}\n${code}\n\`\`\``;
                const response = await client.runAgent(prompt);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/refactor',
            description: 'Refatora a seleção',
            handler: async (args, ctx, client) => {
                const code = args || ctx.selection || ctx.content?.substring(0, 2000) || '';
                const prompt = `Refatore este código mantendo o mesmo comportamento:\n\`\`\`${ctx.language || ''}\n${code}\n\`\`\``;
                const response = await client.runAgent(prompt);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/review',
            description: 'Revisão de código detalhada',
            handler: async (args, ctx, client) => {
                const code = args || ctx.content?.substring(0, 3000) || '';
                const prompt = `Faça uma revisão de código detalhada:\n\`\`\`${ctx.language || ''}\n${code}\n\`\`\``;
                const response = await client.runAgent(prompt);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/doc',
            description: 'Gera documentação do código',
            handler: async (args, ctx, client) => {
                const code = args || ctx.selection || ctx.content?.substring(0, 2000) || '';
                const prompt = `Gere documentação para este código:\n\`\`\`${ctx.language || ''}\n${code}\n\`\`\``;
                const response = await client.runAgent(prompt);
                return response.narration || response.error || 'Sem resposta';
            }
        });

        this.register({
            id: '/run',
            description: 'Executa comando no terminal (ex: /run npm test)',
            handler: async (args, ctx, client) => {
                if (!args) return 'Uso: /run <comando>. Exemplo: /run npm test';
                if (!this._terminalManager) return 'TerminalManager não disponível';
                const result = await this._terminalManager.runCommand(args);
                return result.exitCode === 1 && result.stderr
                    ? `❌ ${result.stderr}`
                    : `✅ ${result.stdout}`;
            }
        });

        this.register({
            id: '/build',
            description: 'Executa build do projeto (dotnet build, npm run build, etc.)',
            handler: async (args, ctx, client) => {
                if (!this._terminalManager) return 'TerminalManager não disponível';
                const lang = (ctx.language || '').toLowerCase();
                let cmd = args;
                if (!cmd) {
                    if (lang.includes('c#') || lang.includes('csharp') || ctx.activeFile?.endsWith('.csproj') || ctx.activeFile?.endsWith('.sln')) {
                        cmd = 'dotnet build';
                    } else if (lang.includes('typescript') || lang.includes('javascript') || ctx.activeFile?.endsWith('package.json')) {
                        cmd = 'npm run build';
                    } else if (lang.includes('python')) {
                        cmd = 'python -m build';
                    } else {
                        return 'Não foi possível detectar o comando de build. Use /run <comando>.';
                    }
                }
                const result = await this._terminalManager.runCommand(cmd);
                return result.exitCode === 1 && result.stderr
                    ? `❌ Build falhou:\n${result.stderr}`
                    : `✅ Build concluído:\n${result.stdout || cmd}`;
            }
        });

        this.register({
            id: '/test-cmd',
            description: 'Executa testes no terminal (ex: /test-cmd npm test)',
            handler: async (args, ctx, client) => {
                if (!this._terminalManager) return 'TerminalManager não disponível';
                const lang = (ctx.language || '').toLowerCase();
                let cmd = args;
                if (!cmd) {
                    if (lang.includes('c#') || lang.includes('csharp')) {
                        cmd = 'dotnet test';
                    } else if (lang.includes('typescript') || lang.includes('javascript')) {
                        cmd = 'npm test';
                    } else if (lang.includes('python')) {
                        cmd = 'python -m pytest';
                    } else {
                        return 'Não foi possível detectar o comando de teste. Use /run <comando>.';
                    }
                }
                const result = await this._terminalManager.runCommand(cmd);
                return result.exitCode === 1 && result.stderr
                    ? `❌ Testes falharam:\n${result.stderr}`
                    : `✅ Testes executados:\n${result.stdout || cmd}`;
            }
        });

        // Git commands
        this.register({
            id: '/commit',
            description: 'Cria commit (ex: /commit fix: corrige bug de login)',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                if (!args) return 'Uso: /commit <mensagem>. Exemplo: /commit fix: corrige bug de login';
                const result = await this._gitManager.commit(args);
                return result.success
                    ? `✅ Commit criado:\n${result.output}`
                    : `❌ Erro ao criar commit:\n${result.error || result.output}`;
            }
        });

        this.register({
            id: '/diff',
            description: 'Mostra diff de mudanças não commitadas',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                const staged = args === '--staged' || args === '--cached';
                const result = await this._gitManager.getDiff(staged);
                return result.success
                    ? `📊 Diff${staged ? ' (staged)' : ''}:\n\`\`\`diff\n${result.output || '(sem mudanças)'}\n\`\`\``
                    : `❌ Erro ao obter diff:\n${result.error || result.output}`;
            }
        });

        this.register({
            id: '/branch',
            description: 'Lista branches ou cria nova (ex: /branch, /branch feature/x)',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                if (args) {
                    const result = await this._gitManager.createBranch(args);
                    return result.success
                        ? `✅ Branch criada: ${args}`
                        : `❌ Erro ao criar branch:\n${result.error || result.output}`;
                }
                const branches = await this._gitManager.getBranches();
                if (!branches.length) return 'Nenhuma branch encontrada';
                return branches.map(b =>
                    `${b.current ? '* ' : '  '}${b.name}`
                ).join('\n');
            }
        });

        this.register({
            id: '/status',
            description: 'Mostra status do git (arquivos modificados)',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                const status = await this._gitManager.getStatus();
                const branch = await this._gitManager.getCurrentBranch();
                const header = `📂 Branch: ${branch}\n`;
                if (!status.success) return `${header}❌ ${status.error || 'Erro ao obter status'}`;
                return `${header}\`\`\`\n${status.output || '(working tree limpo)'}\n\`\`\``;
            }
        });

        this.register({
            id: '/log',
            description: 'Mostra histórico de commits recentes',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                const count = parseInt(args) || 10;
                const result = await this._gitManager.getLog(count);
                return result.success
                    ? `📜 Últimos ${count} commits:\n\`\`\`\n${result.output}\n\`\`\``
                    : `❌ ${result.error || 'Erro ao obter log'}`;
            }
        });

        this.register({
            id: '/review-pr',
            description: 'Revisa um PR (ex: /review-pr 123)',
            handler: async (args, ctx, client) => {
                if (!this._gitManager) return 'GitManager não disponível';
                const prNumber = parseInt(args);
                if (isNaN(prNumber)) return 'Uso: /review-pr <número>. Exemplo: /review-pr 123';
                return await this._gitManager.reviewPR(prNumber);
            }
        });

        // Agentic Loop
        this.register({
            id: '/task',
            description: 'Executa tarefa multi-passo com loop agente (ex: /task adicione testes para auth)',
            handler: async (args, ctx, client) => {
                if (!this._loopManager) return 'AgenticLoopManager não disponível (requer codingAgent.agenticLoops=true)';
                if (!args) return 'Uso: /task <descrição da tarefa>. Exemplo: /task adicione testes para o módulo de autenticação';
                const result = await this._loopManager.executeTask(args, ctx);
                return this._loopManager.formatResult(result);
            }
        });
    }

    register(cmd: SlashCommand) {
        this._commands.set(cmd.id, cmd);
    }

    get(id: string): SlashCommand | undefined {
        return this._commands.get(id);
    }

    getAll(): SlashCommand[] {
        return Array.from(this._commands.values());
    }

    getCompletionItems(): vscode.CompletionItem[] {
        return this.getAll().map(cmd => {
            const item = new vscode.CompletionItem(cmd.id, vscode.CompletionItemKind.Snippet);
            item.detail = cmd.description;
            item.insertText = cmd.id + ' ';
            return item;
        });
    }

    async execute(input: string, context: EditorContext): Promise<string> {
        const trimmed = input.trim();
        for (const [id, cmd] of this._commands) {
            if (trimmed.startsWith(id)) {
                const args = trimmed.slice(id.length).trim();
                return cmd.handler(args, context, this._client);
            }
        }
        throw new Error(`Comando não encontrado: ${trimmed.split(' ')[0]}`);
    }

    parse(input: string): { command?: string; args: string; rest: string } {
        const trimmed = input.trim();
        if (!trimmed.startsWith('/')) return { args: '', rest: trimmed };
        const firstSpace = trimmed.indexOf(' ');
        const cmd = firstSpace === -1 ? trimmed : trimmed.substring(0, firstSpace);
        const args = firstSpace === -1 ? '' : trimmed.substring(firstSpace + 1).trim();
        return {
            command: this._commands.has(cmd) ? cmd : undefined,
            args,
            rest: this._commands.has(cmd) ? '' : trimmed
        };
    }
}
