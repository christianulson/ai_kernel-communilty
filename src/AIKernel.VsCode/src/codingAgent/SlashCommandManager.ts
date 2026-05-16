import * as vscode from 'vscode';
import { EditorContext } from './EditorContextProvider';
import { KernelClient } from '../api/client';

export type SlashCommandHandler = (args: string, context: EditorContext, client: KernelClient) => Promise<string>;

export interface SlashCommand {
    id: string;
    description: string;
    handler: SlashCommandHandler;
}

export class SlashCommandManager {
    private _commands = new Map<string, SlashCommand>();
    private _client: KernelClient;

    constructor(client: KernelClient) {
        this._client = client;
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
