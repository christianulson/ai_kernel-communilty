jest.mock('vscode', () => {
    const mockParticipant = {
        onDidReceiveFeedback: jest.fn(),
        commandProvider: {
            provideCommands: jest.fn().mockReturnValue([]),
        },
    };
    return {
        window: {
            createOutputChannel: jest.fn().mockReturnValue({ trace: jest.fn(), info: jest.fn(), warn: jest.fn(), error: jest.fn() }),
        },
        chat: {
            createChatParticipant: jest.fn().mockReturnValue(mockParticipant),
        },
        ThemeIcon: jest.fn().mockImplementation((id: string) => ({ id })),
        ChatResultFeedbackKind: { Helpful: 1, Unhelpful: 2 },
    };
}, { virtual: true });

import { registerKernelChatParticipant } from '../chat/KernelChatParticipant';
import { KernelClient } from '../api/client';
import { ApprovalManager, ApprovalMode } from '../codingAgent/ApprovalManager';
import { SessionManager } from '../services/SessionManager';

jest.mock('../api/client');

describe('KernelChatParticipant', () => {
    let mockContext: any;
    let mockClient: jest.Mocked<KernelClient>;
    let approvalManager: ApprovalManager;
    let sessionManager: SessionManager;

    beforeEach(() => {
        jest.clearAllMocks();
        mockContext = {
            subscriptions: [],
            globalState: {
                get: jest.fn().mockReturnValue([]),
                update: jest.fn().mockResolvedValue(undefined),
            },
        };
        mockClient = new KernelClient() as jest.Mocked<KernelClient>;
        mockClient.runAgent = jest.fn().mockResolvedValue({ narration: 'resposta' });
        approvalManager = new ApprovalManager();
        sessionManager = new SessionManager(mockContext);
    });

    it('registerKernelChatParticipant_ShouldCreateParticipant', () => {
        const result = registerKernelChatParticipant(mockContext, mockClient, approvalManager, sessionManager);
        const { chat } = require('vscode');
        expect(chat.createChatParticipant).toHaveBeenCalledWith('aikernel.coding', expect.any(Function));
        expect(result).toBeDefined();
    });

    it('registerKernelChatParticipant_ShouldSetIcon', () => {
        registerKernelChatParticipant(mockContext, mockClient, approvalManager, sessionManager);
        const { chat } = require('vscode');
        const participant = chat.createChatParticipant.mock.results[0].value;
        expect(participant.iconPath).toBeDefined();
    });

    it('registerKernelChatParticipant_ShouldRegisterCommandProvider', () => {
        registerKernelChatParticipant(mockContext, mockClient, approvalManager, sessionManager);
        const { chat } = require('vscode');
        const participant = chat.createChatParticipant.mock.results[0].value;
        const commands = participant.commandProvider.provideCommands();
        expect(commands.length).toBeGreaterThanOrEqual(6);
        expect(commands[0].id).toBe('/explain');
    });

    it('registerKernelChatParticipant_ShouldAddToContextSubscriptions', () => {
        registerKernelChatParticipant(mockContext, mockClient, approvalManager, sessionManager);
        expect(mockContext.subscriptions.length).toBe(1);
    });

    it('registerKernelChatParticipant_ShouldCreateOutputChannel', () => {
        registerKernelChatParticipant(mockContext, mockClient, approvalManager, sessionManager);
        const { window } = require('vscode');
        expect(window.createOutputChannel).toHaveBeenCalledWith('AI Kernel Chat', { log: true });
    });
});
