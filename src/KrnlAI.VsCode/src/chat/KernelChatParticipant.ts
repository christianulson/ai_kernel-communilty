import * as vscode from 'vscode';
import { KernelClient } from '../api/client';
import { ApprovalManager, ApprovalMode } from '../codingAgent/ApprovalManager';
import { SessionManager } from '../services/SessionManager';
import { ChatMessage, createMessage } from './ChatMessage';

export function registerKernelChatParticipant(
    context: vscode.ExtensionContext,
    client: KernelClient,
    approvalManager: ApprovalManager,
    sessionManager?: SessionManager
): vscode.Disposable {
    const participant = vscode.chat.createChatParticipant('krnlai.coding', async (request, _ctx, stream, token) => {
        const sessionMessages: ChatMessage[] = [];

        const handleStream = (chunk: string) => {
            stream.markdown(chunk);
        };

        const userMsg = createMessage('user', request.prompt);
        sessionMessages.push(userMsg);

        const command = request.command;
        if (command) {
            const assistantMsg = createMessage('assistant', `Executando comando: ${command}...`);
            sessionMessages.push(assistantMsg);
            stream.markdown(`Executando **/${command}**...\n\n`);
        }

        const mode = approvalManager.getMode();
        if (mode !== ApprovalMode.Chat) {
            stream.markdown(`> Modo: **${mode}**\n\n`);
        }

        try {
            const prompt = command
                ? `/${command} ${request.prompt}`
                : request.prompt;

            const response = await client.runAgent(prompt, 'gateway');

            if (token.isCancellationRequested) return;

            const text = response.narration || response.error || 'Sem resposta';

            if (response.error) {
                stream.markdown(`❌ **Erro:** ${text}`);
                sessionMessages.push(createMessage('assistant', text, undefined));
            } else {
                stream.markdown(text);
                sessionMessages.push(createMessage('assistant', text, command ? { command } : undefined));
            }

            if (sessionManager && sessionMessages.length > 0) {
                await sessionManager.autoSave(
                    `Chat ${new Date().toLocaleDateString()}`,
                    sessionMessages
                );
            }
        } catch (err: any) {
            stream.markdown(`❌ **Erro de conexão:** ${err.message}`);
            sessionMessages.push(createMessage('system', `Erro: ${err.message}`, undefined));
        }
    });

    participant.iconPath = new vscode.ThemeIcon('hubot');

    participant.onDidReceiveFeedback((feedback) => {
        const emoji = feedback.kind === vscode.ChatResultFeedbackKind.Helpful ? '👍' : '👎';
        console.log(`[AI Kernel] Chat feedback: ${emoji} - ${feedback.result?.metadata ?? ''}`);
    });

    (participant as any).commandProvider = {
        provideCommands: () => [
            { id: '/explain', description: 'Explain selected code' },
            { id: '/fix', description: 'Fix code issues' },
            { id: '/test', description: 'Generate unit tests' },
            { id: '/refactor', description: 'Refactor selected code' },
            { id: '/review', description: 'Review code' },
            { id: '/doc', description: 'Generate documentation' },
        ]
    };

    const logger = vscode.window.createOutputChannel('AI Kernel Chat', { log: true });

    context.subscriptions.push(participant);

    return participant;
}
